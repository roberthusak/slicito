using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;

internal class ProductionSlice(ISlice originalSlice, DotNetSolutionContext solutionContext) : ISlice
{
    public SliceSchema Schema => originalSlice.Schema;

    public Func<ElementId, ValueTask<string>> GetElementAttributeProviderAsyncCallback(string attributeName)
    {
        return originalSlice.GetElementAttributeProviderAsyncCallback(attributeName);
    }

    public ElementType GetElementType(ElementId elementId)
    {
        return originalSlice.GetElementType(elementId);
    }

    public ILazyLinkExplorer GetLinkExplorer(LinkType? linkType = null, ElementType? sourceType = null, ElementType? targetType = null)
    {
        return originalSlice.GetLinkExplorer(linkType, sourceType, targetType);
    }

    public async ValueTask<IEnumerable<ElementInfo>> GetRootElementsAsync(ElementType? elementTypeFilter = null)
    {
        var originalElements = await originalSlice.GetRootElementsAsync(elementTypeFilter);

        return originalElements.Where(IsNotTestProject);
    }

    private bool IsNotTestProject(ElementInfo info)
    {
        var project = solutionContext.GetProject(info);

        return !project.Name.Contains("Tests");
    }
}
