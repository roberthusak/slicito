using Slicito.Abstractions;
using Slicito.Queries.Implementation;

namespace Slicito.Queries;

public class SliceBuilder : ISliceBuilder
{
    private readonly Dictionary<ElementType, ISliceBuilder.LoadRootElementsCallback> _rootElementsLoaders = [];

    private readonly Dictionary<(ElementType elementType, string attributeName), ISliceBuilder.LoadElementAttributeCallback> _elementAttributeLoaders = [];

    private readonly Dictionary<LinkType, ISliceBuilder.LoadLinksCallback> _linksLoaders = [];

    public ISliceBuilder AddRootElements(ElementType elementType, ISliceBuilder.LoadRootElementsCallback loader)
    {
        if (_rootElementsLoaders.TryGetValue(elementType, out var existingLoader))
        {
            async ValueTask<IEnumerable<ISliceBuilder.ElementInfo>> MergedLoaderAsync()
            {
                var existingIds = await existingLoader();
                var newIds = await loader();
                return existingIds.Concat(newIds);
            }

            _rootElementsLoaders[elementType] = MergedLoaderAsync;
        }
        else
        {
            _rootElementsLoaders.Add(elementType, loader);
        }

        return this;
    }

    public ISliceBuilder AddElementAttribute(ElementType elementType, string attributeName, ISliceBuilder.LoadElementAttributeCallback loader)
    {
        foreach (var (existingElementType, existingAttributeName) in _elementAttributeLoaders.Keys)
        {
            if (existingAttributeName == attributeName && existingElementType.Value.TryGetIntersection(elementType.Value) is not null)
            {
                throw new InvalidOperationException(
                    $"An attribute loader for attribute '{attributeName}' of element type '{existingElementType}' overlapping with '{elementType}' has already been added.");
            }

        }

        _elementAttributeLoaders.Add((elementType, attributeName), loader);

        return this;
    }

    public ISliceBuilder AddHierarchyLinks(
        LinkType linkType,
        ElementType sourceType,
        ElementType targetType,
        ISliceBuilder.LoadLinksCallback loader)
    {
        return AddLinks(linkType, sourceType, targetType, loader);
    }

    public ISliceBuilder AddLinks(
        LinkType linkType,
        ElementType sourceType,
        ElementType targetType,
        ISliceBuilder.LoadLinksCallback loader)
    {
        if (_linksLoaders.TryGetValue(linkType, out var existingLoader))
        {
            async ValueTask<IEnumerable<ISliceBuilder.LinkInfo>> MergedLoaderAsync(ElementId sourceId)
            {
                var existingIds = await existingLoader(sourceId);
                var newIds = await loader(sourceId);
                return existingIds.Concat(newIds);
            }

            _linksLoaders[linkType] = MergedLoaderAsync;
        }
        else
        {
            _linksLoaders.Add(linkType, loader);
        }

        return this;
    }

    public ISliceBuilder AddLinks(
        LinkType linkType,
        ElementType sourceType,
        ElementType targetType,
        ISliceBuilder.LoadLinkCallback loader)
    {
        if (_linksLoaders.TryGetValue(linkType, out var existingLoader))
        {
            async ValueTask<IEnumerable<ISliceBuilder.LinkInfo>> MergedLoaderAsync(ElementId sourceId)
            {
                var existingIds = await existingLoader(sourceId);
                var newId = await loader(sourceId);

                return newId is not null ? existingIds.Concat([newId.Value]) : existingIds;
            }

            _linksLoaders[linkType] = MergedLoaderAsync;
        }
        else
        {
            async ValueTask<IEnumerable<ISliceBuilder.LinkInfo>> SingleLoaderAsync(ElementId sourceId)
            {
                var newId = await loader(sourceId);

                return newId is not null ?[newId.Value] : [];
            }

            _linksLoaders.Add(linkType, SingleLoaderAsync);
        }

        return this;
    }

    public ILazySlice BuildLazy() => new LazySlice(
        _rootElementsLoaders,
        _elementAttributeLoaders,
        _linksLoaders);
}
