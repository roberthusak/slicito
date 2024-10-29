namespace Slicito.Abstractions;

public interface ISliceBuilder
{
    public record struct ElementInfo(ElementId Id, ElementType? DetailedType = null);

    public record struct LinkInfo(ElementInfo Target, LinkType? DetailedType = null);

    public delegate ValueTask<IEnumerable<ElementInfo>> LoadRootElementsCallback();

    public delegate ValueTask<string> LoadElementAttributeCallback(ElementId elementId);

    public delegate ValueTask<IEnumerable<LinkInfo>> LoadLinksCallback(ElementId sourceId);

    public delegate ValueTask<LinkInfo?> LoadLinkCallback(ElementId sourceId);

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
