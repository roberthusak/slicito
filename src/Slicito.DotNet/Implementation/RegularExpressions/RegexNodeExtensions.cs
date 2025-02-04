using System.Collections.Immutable;

using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;

namespace Slicito.DotNet.Implementation.RegularExpressions;

using RegexAlternatingSequenceList = EmbeddedSeparatedSyntaxNodeList<RegexKind, RegexNode, RegexSequenceNode>;

internal static class RegexNodeExtensions
{
    public static RegexCompilationUnit WithExpression(this RegexCompilationUnit compilationUnit, RegexExpressionNode expression)
    {
        if (compilationUnit.Expression == expression)
        {
            return compilationUnit;
        }

        return new RegexCompilationUnit(expression, compilationUnit.EndOfFileToken);
    }

    public static RegexSequenceNode WithChildren(this RegexSequenceNode sequence, ImmutableArray<RegexExpressionNode> children)
    {
        if (sequence.Children == children)
        {
            return sequence;
        }

        return new RegexSequenceNode(children);
    }

    public static RegexAlternationNode WithSequenceList(this RegexAlternationNode alternation, RegexAlternatingSequenceList sequenceList)
    {
        if (alternation.SequenceList.NodesAndTokens == sequenceList.NodesAndTokens)
        {
            return alternation;
        }

        return new RegexAlternationNode(sequenceList);
    }

    public static ImmutableArray<RegexExpressionNode> ReplaceNodes(
        this ImmutableArray<RegexExpressionNode> children,
        Func<RegexNode, RegexNode> transformer)
    {
        ImmutableArray<RegexExpressionNode>.Builder? builder = null;

        for (var i = 0; i < children.Length; i++)
        {
            var transformedNode = transformer(children[i]);

            if (transformedNode != children[i])
            {
                builder ??= children.ToBuilder();
                builder[i] = (RegexExpressionNode) transformedNode;
            }
        }

        return builder?.ToImmutable() ?? children;
    }


    public static RegexAlternatingSequenceList ReplaceNodes(this RegexAlternatingSequenceList sequenceList, Func<RegexNode, RegexNode> transformer)
    {
        ImmutableArray<EmbeddedSyntaxNodeOrToken<RegexKind, RegexNode>>.Builder? builder = null;

        for (var i = 0; i < sequenceList.NodesAndTokens.Length; i++)
        {
            if (sequenceList.NodesAndTokens[i].IsNode)
            {
                var transformedNode = transformer(sequenceList.NodesAndTokens[i].Node!);

                if (transformedNode != sequenceList.NodesAndTokens[i].Node)
                {
                    builder ??= sequenceList.NodesAndTokens.ToBuilder();
                    builder[i] = (RegexExpressionNode) transformedNode;
                }
            }
        }

        return new RegexAlternatingSequenceList(builder?.ToImmutable() ?? sequenceList.NodesAndTokens);
    }
}
