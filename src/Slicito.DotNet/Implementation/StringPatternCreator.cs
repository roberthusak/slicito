using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

using Slicito.DotNet.Implementation.RegularExpressions;
using Slicito.ProgramAnalysis.Strings;

namespace Slicito.DotNet.Implementation;

internal class StringPatternCreator
{
    public static StringPattern ParseRegex(string value)
    {
        var chars = VirtualCharSequence.Create(0, value);

        var tree = RegexParser.TryParse(chars, RegexOptions.None)
            ?? throw new ArgumentException("Cannot parse regex pattern.", nameof(value));

        if (tree.Diagnostics.Any())
        {
            var errors = string.Join(" ", tree.Diagnostics.Select(d =>
                $"({d.Span.Start}-{d.Span.End}): {d.Message}"));

            throw new ArgumentException($"Error(s) occurred while parsing regex pattern: {errors}", nameof(value));
        }

        var anchorRemover = new AnchorRemover();

        var root = anchorRemover.RemoveAnchors(tree.Root);

        var pattern = ParseRegexNode(root);

        if (!anchorRemover.HadStartAnchor)
        {
            pattern = new StringPattern.Sequence(StringPattern.All.Instance, pattern);
        }

        if (!anchorRemover.HadEndAnchor)
        {
            pattern = new StringPattern.Sequence(pattern, StringPattern.All.Instance);
        }

        return pattern;
    }

    private static StringPattern ParseRegexNode(RegexNode node)
    {
        // The order matches the one in IRegexNodeVisitor
        return node switch
        {
            RegexCompilationUnit compilationUnit => ParseRegexNode(compilationUnit.Expression),

            RegexSequenceNode sequence => FoldNodes(
                sequence.Children,
                ParseRegexNode,
                (left, right) => new StringPattern.Sequence(left, right)),

            RegexTextNode text => new StringPattern.Literal(text.TextToken.ToString()),

            RegexCharacterClassNode characterClass => new StringPattern.Character(ParseCharacterClass(characterClass.Components)),

            RegexWildcardNode wildcard => new StringPattern.Character(CharacterClass.Any.Instance),

            RegexZeroOrMoreQuantifierNode zeroOrMore => new StringPattern.Loop(ParseRegexNode(zeroOrMore.Expression), 0, null),

            RegexOneOrMoreQuantifierNode oneOrMore => new StringPattern.Loop(ParseRegexNode(oneOrMore.Expression), 1, null),

            RegexZeroOrOneQuantifierNode zeroOrOne => new StringPattern.Loop(ParseRegexNode(zeroOrOne.Expression), 0, 1),

            RegexLazyQuantifierNode lazy => ParseRegexNode(lazy.Quantifier),

            RegexExactNumericQuantifierNode n => new StringPattern.Loop(
                ParseRegexNode(n.Expression),
                ParseNumberToken(n.FirstNumberToken),
                ParseNumberToken(n.FirstNumberToken)),

            RegexOpenNumericRangeQuantifierNode nOrMore => new StringPattern.Loop(
                ParseRegexNode(nOrMore.Expression),
                ParseNumberToken(nOrMore.FirstNumberToken),
                null),

            RegexClosedNumericRangeQuantifierNode nToM => new StringPattern.Loop(
                ParseRegexNode(nToM.Expression),
                ParseNumberToken(nToM.FirstNumberToken),
                ParseNumberToken(nToM.SecondNumberToken)),

            RegexAnchorNode anchor => throw new NotSupportedException("Anchors are supported only at the beginning or end of the pattern."),

            RegexAlternationNode alternation => FoldNodes(
                alternation.SequenceList,
                ParseRegexNode,
                (left, right) => new StringPattern.Alternation(left, right)),

            RegexSimpleGroupingNode simpleGroup => ParseRegexNode(simpleGroup.Expression),

            RegexNonCapturingGroupingNode nonCapturingGroup => ParseRegexNode(nonCapturingGroup.Expression),

            _ => throw new NotSupportedException($"Regex node kind '{node.Kind}' (type: '{node.GetType().Name}') is not supported."),
        };

        static int ParseNumberToken(EmbeddedSyntaxToken<RegexKind> token) => int.Parse(token.ToString());
    }

    private static CharacterClass ParseCharacterClass(RegexNode node)
    {
        // The order matches the one in IRegexNodeVisitor (contains only nodes expected to be present in character classes)
        return node switch
        {
            RegexSequenceNode sequence => FoldNodes(
                sequence.Children,
                ParseCharacterClass,
                (left, right) => new CharacterClass.Union(left, right)),

            RegexTextNode text => SplitTextToSingleCharactersUnion(text.TextToken.ToString()),

            RegexCharacterClassRangeNode range => new CharacterClass.Range(
                ParseCharacterRangeLimit(range.Left),
                ParseCharacterRangeLimit(range.Right)),

            _ => throw new NotSupportedException($"Regex node kind '{node.Kind}' (type: '{node.GetType().Name}') is not supported in character classes."),
        };
    }

    private static CharacterClass SplitTextToSingleCharactersUnion(string text)
    {
        if (text == "")
        {
            throw new ArgumentException("Unexpected empty text of character class.", nameof(text));
        }

        return text.Skip(1).Aggregate<char, CharacterClass>(
            new CharacterClass.Single(text[0]),
            (left, c) => new CharacterClass.Union(left, new CharacterClass.Single(c)));
    }

    private static char ParseCharacterRangeLimit(RegexNode node)
    {
        var characterClass = ParseCharacterClass(node);

        if (characterClass is not CharacterClass.Single single)
        {
            throw new NotSupportedException($"Only single character classes are supported as range limits.");
        }

        return single.Value;
    }

    private static T FoldNodes<T>(
        EmbeddedSeparatedSyntaxNodeList<RegexKind, RegexNode, RegexSequenceNode> sequenceList,
        Func<RegexNode, T> transformer,
        Func<T, T, T> folder)

    {
        var nodes = sequenceList.NodesAndTokens
            .Select(n => n.Node)
            .OfType<RegexExpressionNode>()
            .ToList();

        return FoldNodes(nodes, transformer, folder);
    }

    private static T FoldNodes<T>(IEnumerable<RegexExpressionNode> nodes, Func<RegexNode, T> transformer, Func<T, T, T> folder)
    {
        if (!nodes.Any())
        {
            throw new ArgumentException("Unexpected empty sequence of regex nodes.", nameof(nodes));
        }

        return nodes.Skip(1).Aggregate(
            transformer(nodes.First()),
            (pattern, node) =>
                folder(pattern, transformer(node)));
    }

    private class AnchorRemover
    {
        public bool HadStartAnchor { get; private set; }
        public bool HadEndAnchor { get; private set; }

        public RegexNode RemoveAnchors(RegexNode node)
        {
            return node switch
            {
                RegexCompilationUnit compilationUnit => compilationUnit.WithExpression(
                    (RegexExpressionNode) RemoveAnchors(compilationUnit.Expression)),

                RegexSequenceNode sequence => ProcessSequence(sequence),

                RegexAlternationNode alternation => ProcessAlternation(alternation),

                _ => node,
            };
        }

        private RegexSequenceNode ProcessSequence(RegexSequenceNode sequence)
        {
            if (sequence.Children.Length == 0)
            {
                throw new ArgumentException("Unexpected empty sequence.", nameof(sequence));
            }
            else if (sequence.Children.Length == 1)
            {
                sequence.WithChildren(sequence.Children.ReplaceNodes(RemoveAnchors));
            }

            if (sequence.Children[0].Kind == RegexKind.StartAnchor)
            {
                HadStartAnchor = true;
            }

            if (sequence.Children[^1].Kind == RegexKind.EndAnchor)
            {
                HadEndAnchor = true;
            }

            if (!HadStartAnchor && !HadEndAnchor)
            {
                return sequence;
            }

            var childrenBuilder = sequence.Children.ToBuilder();

            if (HadStartAnchor)
            {
                childrenBuilder.RemoveAt(0);
            }

            if (HadEndAnchor)
            {
                childrenBuilder.RemoveAt(childrenBuilder.Count - 1);
            }

            return new RegexSequenceNode(childrenBuilder.ToImmutable());
        }


        private RegexAlternationNode ProcessAlternation(RegexAlternationNode alternation)
        {
            var nodeCount = alternation.SequenceList.NodesAndTokens.Count(nt => nt.IsNode);

            if (nodeCount == 0)
            {
                throw new ArgumentException("Unexpected empty alternation.", nameof(alternation));
            }
            else if (nodeCount > 1)
            {
                // We skip the search for anchors in alternations with more than one node as it would be too complex
                // (if there are any anchors, parser will throw an error later)
                return alternation;
            }

            return alternation.WithSequenceList(
                    alternation.SequenceList.ReplaceNodes(RemoveAnchors));
        }
    }
}
