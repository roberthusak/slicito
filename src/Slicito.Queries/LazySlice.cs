using Slicito.Abstractions;
using Slicito.Abstractions.Queries;

namespace Slicito.Queries;

internal class LazySlice : ILazySlice
{
    private readonly Dictionary<ElementType, ISliceBuilder.LoadRootElementsCallback> _rootElementsLoaders;
    private readonly Dictionary<(ElementType, string attributeName), ISliceBuilder.LoadElementAttributeCallback> _elementAttributeLoaders;
    private readonly Dictionary<LinkType, ISliceBuilder.LoadLinksCallback> _linksLoaders;
    private readonly Dictionary<LinkType, ISliceBuilder.LoadLinkCallback> _linkLoaders;

    public LazySlice(
        Dictionary<ElementType, ISliceBuilder.LoadRootElementsCallback> rootElementsLoaders,
        Dictionary<(ElementType, string attributeName), ISliceBuilder.LoadElementAttributeCallback> elementAttributeLoaders,
        Dictionary<LinkType, ISliceBuilder.LoadLinksCallback> linksLoaders,
        Dictionary<LinkType, ISliceBuilder.LoadLinkCallback> linkLoaders)
    {
        _rootElementsLoaders = rootElementsLoaders;
        _elementAttributeLoaders = elementAttributeLoaders;
        _linksLoaders = linksLoaders;
        _linkLoaders = linkLoaders;
    }

    public async ValueTask<IEnumerable<ElementId>> GetRootElementIdsAsync(ElementType? elementTypeFilter = null)
    {
        var result = Enumerable.Empty<ElementId>();

        foreach (var kvp in _rootElementsLoaders)
        {
            IEnumerable<ISliceBuilder.ElementInfo> elementInfos;

            if (elementTypeFilter is not null)
            {
                var typeFilter = elementTypeFilter.Value.Value;

                if (typeFilter.TryGetIntersection(kvp.Key.Value) is null)
                {
                    continue;
                }
                else if (typeFilter.IsStrictSubsetOf(kvp.Key.Value))
                {
                    elementInfos = (await kvp.Value())
                        .Where(info => info.DetailedType?.Value.IsSubsetOfOrEquals(typeFilter) ?? false);
                }
                else
                {
                    elementInfos = await kvp.Value();
                }
            }
            else
            {
                elementInfos = await kvp.Value();
            }

            result = result.Concat(elementInfos.Select(i => i.Id));
        }

        return result;
    }

    public Func<ElementId, ValueTask<string>> GetElementAttributeProviderAsyncCallback(string attributeName)
    {
        throw new NotImplementedException();
    }

    public ILazyLinkExplorer GetLinkExplorer(LinkType? linkType = null, ElementType? sourceType = null, ElementType? targetType = null)
    {
        throw new NotImplementedException();
    }
}
