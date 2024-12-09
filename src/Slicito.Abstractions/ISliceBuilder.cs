namespace Slicito.Abstractions;

public interface ISliceBuilder
{
    public record struct PartialElementInfo(ElementId Id, ElementType? DetailedType = null);

    public record struct PartialLinkInfo(PartialElementInfo Target, LinkType? DetailedType = null);

    public delegate ValueTask<IEnumerable<PartialElementInfo>> LoadRootElementsAsyncCallback();

    public delegate ValueTask<string> LoadElementAttributeAsyncCallback(ElementId elementId);

    public delegate ValueTask<IEnumerable<PartialLinkInfo>> LoadLinksAsyncCallback(ElementId sourceId);

    public delegate ValueTask<PartialLinkInfo?> LoadLinkAsyncCallback(ElementId sourceId);

    ISliceBuilder AddRootElements(ElementType elementType, LoadRootElementsAsyncCallback loader);

    ISliceBuilder AddElementAttribute(ElementType elementType, string attributeName, LoadElementAttributeAsyncCallback loader);

    ISliceBuilder AddHierarchyLinks(
        LinkType linkType,
        ElementType sourceType,
        ElementType targetType,
        LoadLinksAsyncCallback loader);

    ISliceBuilder AddLinks(
        LinkType linkType,
        ElementType sourceType,
        ElementType targetType,
        LoadLinksAsyncCallback loader);

    ISliceBuilder AddLinks(
        LinkType linkType,
        ElementType sourceType,
        ElementType targetType,
        LoadLinkAsyncCallback loader);

    ILazySlice BuildLazy();
}
