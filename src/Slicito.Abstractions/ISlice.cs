namespace Slicito.Abstractions;

public interface ISlice
{
    SliceSchema Schema { get; }

    ValueTask<IEnumerable<ElementInfo>> GetRootElementsAsync(ElementType? elementTypeFilter = null);

    ElementType GetElementType(ElementId elementId);

    Func<ElementId, ValueTask<string>> GetElementAttributeProviderAsyncCallback(string attributeName);

    ILazyLinkExplorer GetLinkExplorer(LinkType? linkType = null, ElementType? sourceType = null, ElementType? targetType = null);
}
