namespace Slicito.Abstractions;

public interface ILazySlice
{
    ValueTask<IEnumerable<ElementId>> GetRootElementIdsAsync(ElementType? elementTypeFilter = null);

    Func<ElementId, ValueTask<string>> GetElementAttributeProviderAsyncCallback(string attributeName);

    ILazyLinkExplorer GetLinkExplorer(LinkType? linkType = null, ElementType? sourceType = null, ElementType? targetType = null);
}
