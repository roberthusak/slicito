using System.Collections.Concurrent;

using Slicito.Abstractions;
using Slicito.Abstractions.Queries;

namespace Slicito.Queries.Implementation;

internal class LazySlice : ILazySlice
{
    private readonly Dictionary<ElementType, ISliceBuilder.LoadRootElementsCallback> _rootElementsLoaders;
    private readonly Dictionary<(ElementType elementType, string attributeName), ISliceBuilder.LoadElementAttributeCallback> _elementAttributeLoaders;
    private readonly Dictionary<LinkType, ISliceBuilder.LoadLinksCallback> _linksLoaders;

    private readonly ConcurrentDictionary<ElementId, ElementType> _elementTypes = new();

    public LazySlice(
        Dictionary<ElementType, ISliceBuilder.LoadRootElementsCallback> rootElementsLoaders,
        Dictionary<(ElementType, string attributeName), ISliceBuilder.LoadElementAttributeCallback> elementAttributeLoaders,
        Dictionary<LinkType, ISliceBuilder.LoadLinksCallback> linksLoaders)
    {
        _rootElementsLoaders = rootElementsLoaders;
        _elementAttributeLoaders = elementAttributeLoaders;
        _linksLoaders = linksLoaders;
    }

    public async ValueTask<IEnumerable<ElementId>> GetRootElementIdsAsync(ElementType? elementTypeFilter = null)
    {
        var result = Enumerable.Empty<ElementId>();

        foreach (var kvp in _rootElementsLoaders)
        {
            var (groupElementType, loader) = (kvp.Key, kvp.Value);

            IEnumerable<ISliceBuilder.ElementInfo>? elementInfos = null;

            if (elementTypeFilter is not null)
            {
                var typeFilter = elementTypeFilter.Value.Value;

                var restrictedGroupType = typeFilter.TryGetIntersection(groupElementType.Value);
                if (restrictedGroupType is null)
                {
                    // No elements of this group match the filter
                    continue;
                }

                if (!groupElementType.Value.IsSubsetOfOrEquals(restrictedGroupType))
                {
                    // Only elements with more specific types match the filter
                    elementInfos = (await loader())
                        .Where(info => info.DetailedType?.Value.IsSubsetOfOrEquals(restrictedGroupType) ?? false);
                }
            }

            // Otherwise, all elements of this group match the filter (if any)
            elementInfos ??= await loader();

            result = result.Concat(elementInfos.Select(info => CacheType(info, groupElementType).Id));
        }

        return result;
    }

    public Func<ElementId, ValueTask<string>> GetElementAttributeProviderAsyncCallback(string attributeName)
    {
        var typeLoaders = _elementAttributeLoaders
            .Where(kvp => kvp.Key.attributeName == attributeName)
            .ToDictionary(kvp => kvp.Key.elementType, kvp => kvp.Value);

        return async elementId =>
        {
            var elementType = GetElementType(elementId);

            if (!typeLoaders.TryGetValue(elementType, out var loader))
            {
                foreach (var kvp in typeLoaders)
                {
                    var (groupElementType, attributeLoader) = (kvp.Key, kvp.Value);

                    if (elementType.Value.IsSubsetOfOrEquals(groupElementType.Value))
                    {
                        loader = attributeLoader;
                        break;
                    }
                }
            }

            if (loader is null)
            {
                throw new InvalidOperationException(
                    $"The provider of the attribute '{attributeName}' not found for the element '{elementId}' of the type '{elementType}'.");
            }

            return await loader(elementId);
        };
    }

    private ElementType GetElementType(ElementId elementId)
    {
        if (!_elementTypes.TryGetValue(elementId, out var elementType))
        {
            throw new InvalidOperationException($"The type of the element '{elementId}' not found.");
        }

        return elementType;
    }

    public ILazyLinkExplorer GetLinkExplorer(LinkType? linkType = null, ElementType? sourceType = null, ElementType? targetType = null)
    {
        if (sourceType is not null || targetType is not null)
        {
            throw new NotSupportedException("Filtering by source or target type is not supported.");
        }

        var typeLinksLoaders = linkType is null
            ? _linksLoaders
            : _linksLoaders
                .Where(kvp => kvp.Key.Value.TryGetIntersection(linkType.Value.Value) is not null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return new LazyLinkExplorer(this, typeLinksLoaders, linkType);
    }

    private ISliceBuilder.ElementInfo CacheType(ISliceBuilder.ElementInfo elementInfo, ElementType groupType)
    {
        _elementTypes.TryAdd(elementInfo.Id, elementInfo.DetailedType ?? groupType);

        return elementInfo;
    }

    private class LazyLinkExplorer : ILazyLinkExplorer
    {
        private readonly LazySlice _slice;
        private readonly Dictionary<LinkType, ISliceBuilder.LoadLinksCallback> _linksLoaders;
        private readonly LinkType? _linkTypeFilter;

        public LazyLinkExplorer(
            LazySlice slice,
            Dictionary<LinkType, ISliceBuilder.LoadLinksCallback> linksLoaders,
            LinkType? linkTypeFilter)
        {
            _slice = slice;
            _linksLoaders = linksLoaders;
            _linkTypeFilter = linkTypeFilter;
        }

        public async ValueTask<ElementId?> TryGetTargetElementIdAsync(ElementId sourceId)
        {
            var validOrDefaultId = (await GetTargetElementIdsAsync(sourceId)).SingleOrDefault();

            return validOrDefaultId == default ? null : validOrDefaultId;
        }

        public async ValueTask<IEnumerable<ElementId>> GetTargetElementIdsAsync(ElementId sourceId)
        {
            var result = Enumerable.Empty<ElementId>();

            foreach (var kvp in _linksLoaders)
            {
                var (groupLinkType, loader) = (kvp.Key, kvp.Value);

                IEnumerable<ISliceBuilder.LinkInfo>? linkInfos = null;

                if (_linkTypeFilter is not null)
                {
                    var typeFilter = _linkTypeFilter.Value.Value;

                    // The presence of the intersection was already checked before the explorer construction
                    var restrictedGroupType = typeFilter.TryGetIntersection(groupLinkType.Value)!;

                    if (!groupLinkType.Value.IsSubsetOfOrEquals(restrictedGroupType))
                    {
                        // Only links with more specific types match the filter
                        linkInfos = (await loader(sourceId))
                            .Where(info => info.DetailedType?.Value.IsSubsetOfOrEquals(restrictedGroupType) ?? false);
                    }
                }

                // Otherwise, all links of this group match the filter (if any)
                linkInfos ??= await loader(sourceId);

                result = result.Concat(linkInfos.Select(info => info.Target.Id));
            }

            return result;
        }
    }
}
