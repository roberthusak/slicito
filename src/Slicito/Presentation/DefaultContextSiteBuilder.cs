using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.Interactive;

namespace Slicito.Presentation;

public class DefaultContextSiteBuilder
{
    public const string ElementPageId = "element";

    private readonly IContext _context;

    private readonly List<HashSet<IElement>> _elementLevels;

    // FIXME Don't reference SyntaxNode after relation refactoring
    private readonly List<IBinaryRelation<IElement, IElement, SyntaxNode?>> _relations = new();

    public Site.Builder SiteBuilder { get; }

    public DefaultContextSiteBuilder(IContext context)
    {
        _context = context;
        _elementLevels = GenerateElementLevels(context);

        SiteBuilder = new();
    }

    public DefaultContextSiteBuilder AddRelation(IBinaryRelation<IElement, IElement, SyntaxNode?> relation)
    {
        _relations.Add(relation);

        return this;
    }

    public DefaultContextSiteBuilder AddRelations(IEnumerable<IBinaryRelation<IElement, IElement, SyntaxNode?>> relations)
    {
        _relations.AddRange(relations);

        return this;
    }

    public Site Build()
    {
        // FIXME Ensure that the callbacks don't reference mutable fields of this class
        // (store them to an immutable object, or at least reference their copies using a closure

        SiteBuilder
            .AddStaticPage(Site.IndexPageId, options => CreateSchema(options.GetUriDelegate, null))
            .AddDynamicPage(ElementPageId, options =>
            {
                var element = _context.Elements.Single(e => e.Id == options.Parameters["id"]);

                // FIXME Refactor the interfaces so that this ugly code is not needed

                IImmutableDictionary<string, string>? openInIdeParameters = null;

                _context.GetOpenInIdeUriProvider((path, parameters) =>
                {
                    openInIdeParameters = parameters;
                    return null;
                }).TryGetUriForElement(element);

                if (openInIdeParameters is not null)
                {
                    InteractiveSession.Global.OpenFileInIde(
                        openInIdeParameters["path"],
                        int.Parse(openInIdeParameters["line"]),
                        int.Parse(openInIdeParameters["offset"]));
                }

                return CreateSchema(options.GetUriDelegate, element);
            });

        return SiteBuilder.Build();
    }

    private static List<HashSet<IElement>> GenerateElementLevels(IContext context)
    {
        var levels = new List<HashSet<IElement>>();

        var nextLevelStack = new Stack<IElement>(
            context.Elements
            .Where(e => !context.Hierarchy.GetIncoming(e).Any()));

        while (nextLevelStack.Count > 0)
        {
            var level = new HashSet<IElement>();

            var stack = new Stack<IElement>(nextLevelStack);
            nextLevelStack.Clear();

            while (stack.Count > 0)
            {
                var element = stack.Pop();
                level.Add(element);

                foreach (var pair in context.Hierarchy.GetOutgoing(element))
                {
                    if (AreDifferentLevels(element, pair.Target))
                    {
                        nextLevelStack.Push(pair.Target);
                    }
                    else
                    {
                        stack.Push(pair.Target);
                    }
                }
            }

            levels.Add(level);
        }

        return levels;

        bool AreDifferentLevels(IElement parent, IElement child)
        {
            return parent.GetType() != child.GetType();
        }
    }

    private Schema? CreateSchema(GetUriDelegate? getUriDelegate, IElement? rootElement = null)
    {
        // FIXME Refactor

        var elements = new HashSet<IElement>();

        var sameLevelElements = new HashSet<IElement>();
        var underlyingLevelElements = new HashSet<IElement>();

        if (_elementLevels.Count > 0)
        {
            if (rootElement is null)
            {
                underlyingLevelElements = _elementLevels[0];

                elements.UnionWith(underlyingLevelElements);

                if (_elementLevels.Count > 1)
                {
                    elements.UnionWith(_elementLevels[1]);
                }
            }
            else
            {
                elements.Add(rootElement);

                var hierarchySlice = _context.Hierarchy.SliceForward(rootElement);
                var slicedElements = hierarchySlice.GetElements().ToHashSet();

                var rootElementLevel = _elementLevels.FindIndex(level => level.Contains(rootElement));
                sameLevelElements = _elementLevels[rootElementLevel];

                elements.UnionWith(sameLevelElements.Intersect(slicedElements));

                var underlyingLevel = rootElementLevel + 1;
                if (underlyingLevel <= _elementLevels.Count - 1)
                {
                    underlyingLevelElements = _elementLevels[underlyingLevel];

                    elements.UnionWith(underlyingLevelElements.Intersect(slicedElements));
                }

                // Zooming to an element which would be shown as the only one in the view is pointless
                if (elements.Count == 1)
                {
                    return null;
                }
            }
        }

        var summarizedInternalRelation = Relation.Merge(
            _relations.Select(r =>
                r.MoveUpHierarchy(
                    _context.Hierarchy,
                    (_, hierarchyPair) => !underlyingLevelElements.Contains(hierarchyPair.Target)))
            )
            .MakeUnique()
            .Filter(pair => pair.Source != pair.Target && elements.Contains(pair.Source) && elements.Contains(pair.Target));

        var summarizedExternalRelation = Relation.Merge(
            _relations.Select(r =>
                r.MoveUpHierarchy(
                    _context.Hierarchy,
                    (_, hierarchyPair) => !sameLevelElements.Contains(hierarchyPair.Target)))
            )
            .MakeUnique()
            .Filter(pair => elements.Contains(pair.Source) ^ elements.Contains(pair.Target));

        elements.UnionWith(summarizedExternalRelation.GetElements().Intersect(sameLevelElements));

        return new Schema.Builder()
            .AddLabelProvider(_context.LabelProvider)
            .AddNodes(elements, _context.Hierarchy, (e, node) =>
            {
                var parameters = ImmutableDictionary<string, string>.Empty.Add("id", e.Id);
                node.Attr.Uri = getUriDelegate?.Invoke(ElementPageId, parameters)?.ToString();
            })
            .AddEdges(summarizedInternalRelation)
            .AddEdges(summarizedExternalRelation)
            .Build();
    }
}
