using Slicito.Abstractions;

namespace Slicito.Queries;

public class SliceBuilder : ISliceBuilder
{
    private readonly Dictionary<ElementType, ISliceBuilder.LoadRootElementsCallback> _rootElementsLoaders = [];

    private readonly Dictionary<(ElementType, string attributeName), ISliceBuilder.LoadElementAttributeCallback> _elementAttributeLoaders = [];

    private readonly Dictionary<LinkType, ISliceBuilder.LoadLinksCallback> _linksLoaders = [];

    private readonly Dictionary<LinkType, ISliceBuilder.LoadLinkCallback> _linkLoaders = [];

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
                    $"An attribute loader for attribute '{attributeName}' of element type '{existingElementType}' related to '{elementType}' has already been added.");
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
        if (_linkLoaders.ContainsKey(linkType))
        {
            throw new NotSupportedException(
                $"A single link loader for link type '{linkType}' has already been added, the combination with a multiple link loader isn't supported.");
        }

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
        if (_linksLoaders.ContainsKey(linkType))
        {
            throw new NotSupportedException(
                $"A multiple link loader for link type '{linkType}' has already been added, the combination with a single link loader isn't supported.");
        }

        if (_linkLoaders.TryGetValue(linkType, out var existingLoader))
        {
            IEnumerable<ISliceBuilder.LinkInfo> EnumerateTwoLinkInfos(ISliceBuilder.LinkInfo? first, ISliceBuilder.LinkInfo? second)
            {
                if (first is not null)
                {
                    yield return first.Value;
                }

                if (second is not null)
                {
                    yield return second.Value;
                }
            }

            async ValueTask<IEnumerable<ISliceBuilder.LinkInfo>> MergedLoaderAsync(ElementId sourceId)
            {
                var existingId = await existingLoader(sourceId);
                var newId = await loader(sourceId);
                return EnumerateTwoLinkInfos(existingId, newId);
            }

            _linkLoaders.Remove(linkType);
            _linksLoaders[linkType] = MergedLoaderAsync;
        }
        else
        {
            _linkLoaders.Add(linkType, loader);
        }

        return this;
    }

    public ILazySlice BuildLazy() => new LazySlice(
        _rootElementsLoaders,
        _elementAttributeLoaders,
        _linksLoaders,
        _linkLoaders);
}
