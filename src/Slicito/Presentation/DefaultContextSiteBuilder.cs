using System.Collections.Immutable;

using Slicito.Abstractions;
using Slicito.Abstractions.Relations;

namespace Slicito.Presentation;

public class DefaultContextSiteBuilder
{
    public const string ElementPageId = "element";

    private readonly IContext _context;

    private readonly List<HashSet<IElement>> _elementLevels;

    public Site.Builder SiteBuilder { get; }

    public DefaultContextSiteBuilder(IContext context)
    {
        _context = context;
        _elementLevels = GenerateElementLevels(context);

        SiteBuilder = new();
    }

    public Site Build()
    {
        SiteBuilder
            .AddStaticPage(Site.IndexPageId, options => CreateSchema(options.GetUriDelegate, null))
            .AddDynamicPage(ElementPageId, options =>
            {
                var element = _context.Elements.Single(e => e.Id == options.Parameters["id"]);

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

    private Schema CreateSchema(GetUriDelegate? getUriDelegate, IElement? rootElement = null)
    {
        var elements = new HashSet<IElement>();

        if (_elementLevels.Count > 0)
        {
            if (rootElement is null)
            {
                elements.UnionWith(_elementLevels[0]);

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
                elements.UnionWith(_elementLevels[rootElementLevel].Intersect(slicedElements));

                var underlyingLevel = rootElementLevel + 1;
                if (underlyingLevel <= _elementLevels.Count - 1)
                {
                    elements.UnionWith(_elementLevels[underlyingLevel].Intersect(slicedElements));
                }
            }
        }

        return new Schema.Builder()
            .AddLabelProvider(_context.LabelProvider)
            .AddNodes(elements, _context.Hierarchy, (e, node) =>
            {
                var parameters = ImmutableDictionary<string, string>.Empty.Add("id", e.Id);
                node.Attr.Uri = getUriDelegate?.Invoke(ElementPageId, parameters)?.ToString();
            })
            .Build();
    }
}
