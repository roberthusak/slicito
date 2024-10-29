using System.Threading.Tasks;

namespace Slicito.Abstractions;

public interface ISliceBuilder
{
    public delegate ValueTask<IEnumerable<ElementId>> LoadRootElementsCallback();

    public delegate ValueTask<string> LoadElementAttributeCallback(ElementId elementId);

    public delegate ValueTask<IEnumerable<ElementId>> LoadLinksCallback(ElementId sourceId);

    public delegate ValueTask<ElementId?> LoadLinkCallback(ElementId sourceId);

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
