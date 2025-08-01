using System.Collections.Concurrent;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Common.Implementation;

internal class Slice : ISlice
{
    private readonly Dictionary<ElementType, ISliceBuilder.LoadRootElementsAsyncCallback> _rootElementsLoaders;
    private readonly Dictionary<ElementTypeAttribute, ISliceBuilder.LoadElementAttributeAsyncCallback> _elementAttributeLoaders;
    private readonly Dictionary<LinkLoaderTypes, ISliceBuilder.LoadLinksAsyncCallback> _linksLoaders;

    private readonly ConcurrentDictionary<ElementId, ElementType> _elementTypes = new();

    public SliceSchema Schema { get; }

    public Slice(
        SliceSchema schema,
        Dictionary<ElementType, ISliceBuilder.LoadRootElementsAsyncCallback> rootElementsLoaders,
        Dictionary<ElementTypeAttribute, ISliceBuilder.LoadElementAttributeAsyncCallback> elementAttributeLoaders,
        Dictionary<LinkLoaderTypes, ISliceBuilder.LoadLinksAsyncCallback> linksLoaders)
    {
        Schema = schema;

        _rootElementsLoaders = rootElementsLoaders;
        _elementAttributeLoaders = elementAttributeLoaders;
        _linksLoaders = linksLoaders;
    }

    public async ValueTask<IEnumerable<ElementInfo>> GetRootElementsAsync(ElementType? elementTypeFilter = null)
    {
        var result = Enumerable.Empty<ElementInfo>();

        foreach (var kvp in _rootElementsLoaders)
        {
            var (groupElementType, loader) = (kvp.Key, kvp.Value);

            IEnumerable<ISliceBuilder.PartialElementInfo>? elementInfos = null;

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

            result = result.Concat(elementInfos.Select(info => CacheAndIncludeType(info, groupElementType)));
        }

        return result;
    }

    public ElementType GetElementType(ElementId elementId)
    {
        if (!_elementTypes.TryGetValue(elementId, out var elementType))
        {
            throw new InvalidOperationException($"The type of the element '{elementId}' not found.");
        }

        return elementType;
    }

    public Func<ElementId, ValueTask<string>> GetElementAttributeProviderAsyncCallback(string attributeName)
    {
        var typeLoaders = _elementAttributeLoaders
            .Where(kvp => kvp.Key.AttributeName == attributeName)
            .ToDictionary(kvp => kvp.Key.ElementType, kvp => kvp.Value);

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

    public ILazyLinkExplorer GetLinkExplorer(LinkType? linkType = null, ElementType? sourceType = null, ElementType? targetType = null)
    {
        if (sourceType is not null || targetType is not null)
        {
            throw new NotSupportedException("Filtering by source or target type is not supported.");
        }

        var typeLinksLoaders = linkType is null
            ? _linksLoaders
            : _linksLoaders
                .Where(kvp => kvp.Key.LinkType.Value.TryGetIntersection(linkType.Value.Value) is not null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        return new LazyLinkExplorer(this, typeLinksLoaders, linkType);
    }

    private ElementInfo CacheAndIncludeType(ISliceBuilder.PartialElementInfo elementInfo, ElementType groupType)
    {
        var elementType = elementInfo.DetailedType ?? groupType;

        _elementTypes.TryAdd(elementInfo.Id, elementType);

        return new(elementInfo.Id, elementType);
    }

    private class LazyLinkExplorer : ILazyLinkExplorer
    {
        private readonly Slice _slice;
        private readonly Dictionary<LinkLoaderTypes, ISliceBuilder.LoadLinksAsyncCallback> _linksLoaders;
        private readonly LinkType? _linkTypeFilter;

        public LazyLinkExplorer(
            Slice slice,
            Dictionary<LinkLoaderTypes, ISliceBuilder.LoadLinksAsyncCallback> linksLoaders,
            LinkType? linkTypeFilter)
        {
            _slice = slice;
            _linksLoaders = linksLoaders;
            _linkTypeFilter = linkTypeFilter;
        }

        public async ValueTask<ElementInfo?> TryGetTargetElementAsync(ElementId sourceId)
        {
            var validOrDefaultId = (await GetTargetElementsAsync(sourceId)).SingleOrDefault();

            return validOrDefaultId == default ? null : validOrDefaultId;
        }

        public async ValueTask<IEnumerable<ElementInfo>> GetTargetElementsAsync(ElementId sourceId)
        {
            var sourceType = _slice.GetElementType(sourceId);

            var result = Enumerable.Empty<ElementInfo>();

            foreach (var kvp in _linksLoaders)
            {
                var (loaderTypes, loader) = (kvp.Key, kvp.Value);
                var groupLinkType = loaderTypes.LinkType;

                if (!loaderTypes.SourceType.Value.IsSupersetOfOrEquals(sourceType.Value))
                {
                    continue;
                }

                IEnumerable<ISliceBuilder.PartialLinkInfo>? linkInfos = null;

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

                result = result.Concat(linkInfos.Select(info => _slice.CacheAndIncludeType(info.Target, loaderTypes.TargetType)));
            }

            return result;
        }
    }
}
