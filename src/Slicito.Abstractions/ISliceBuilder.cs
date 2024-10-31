namespace Slicito.Abstractions;

public interface ISliceBuilder
{
    public record struct PartialElementInfo(ElementId Id, ElementType? DetailedType = null);

    public record struct PartialLinkInfo(PartialElementInfo Target, LinkType? DetailedType = null);

    public delegate ValueTask<IEnumerable<PartialElementInfo>> LoadRootElementsCallback();

    public delegate ValueTask<string> LoadElementAttributeCallback(ElementId elementId);

    public delegate ValueTask<IEnumerable<PartialLinkInfo>> LoadLinksCallback(ElementId sourceId);

    public delegate ValueTask<PartialLinkInfo?> LoadLinkCallback(ElementId sourceId);

    ISliceBuilder AddRootElements(ElementType elementType, LoadRootElementsCallback loader);

    ISliceBuilder AddElementAttribute(ElementType elementType, string attributeName, LoadElementAttributeCallback loader);

    ISliceBuilder AddHierarchyLinks(
        LinkType linkType,
        ElementType sourceType,
        ElementType targetType,
        LoadLinksCallback loader);

    ISliceBuilder AddLinks(
        LinkType linkType,
        ElementType sourceType,
        ElementType targetType,
        LoadLinksCallback loader);

    ISliceBuilder AddLinks(
        LinkType linkType,
        ElementType sourceType,
        ElementType targetType,
        LoadLinkCallback loader);

    ILazySlice BuildLazy();
}
