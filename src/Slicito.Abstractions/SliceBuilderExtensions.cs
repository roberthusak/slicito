using static Slicito.Abstractions.ISliceBuilder;

namespace Slicito.Abstractions;

public static class SliceBuilderExtensions
{
    public delegate IEnumerable<PartialElementInfo> LoadRootElementsCallback();

    public delegate string LoadElementAttributeCallback(ElementId elementId);

    public delegate IEnumerable<PartialLinkInfo> LoadLinksCallback(ElementId sourceId);

    public delegate PartialLinkInfo? LoadLinkCallback(ElementId sourceId);

    public static ISliceBuilder AddRootElements(
        this ISliceBuilder builder,
        ElementType elementType,
        LoadRootElementsCallback loader)
    {
        return builder.AddRootElements(elementType, () => new(loader()));
    }

    public static ISliceBuilder AddElementAttribute(
        this ISliceBuilder builder,
        ElementType elementType,
        string attributeName,
        LoadElementAttributeCallback loader)
    {
        return builder.AddElementAttribute(elementType, attributeName, elementId => new(loader(elementId)));
    }

    public static ISliceBuilder AddHierarchyLinks(
        this ISliceBuilder builder,
        LinkType linkType,
        ElementType sourceType,
        ElementType targetType,
        LoadLinksCallback loader)
    {
        return builder.AddHierarchyLinks(linkType, sourceType, targetType, elementId => new(loader(elementId)));
    }

    public static ISliceBuilder AddLinks(
        this ISliceBuilder builder,
        LinkType linkType,
        ElementType sourceType,
        ElementType targetType,
        LoadLinksCallback loader)
    {
        return builder.AddLinks(linkType, sourceType, targetType, sourceId => new(loader(sourceId)));
    }

    public static ISliceBuilder AddLinks(
        this ISliceBuilder builder,
        LinkType linkType,
        ElementType sourceType,
        ElementType targetType,
        LoadLinkCallback loader)
    {
        return builder.AddLinks(linkType, sourceType, targetType, sourceId => new(loader(sourceId)));
    }
}
