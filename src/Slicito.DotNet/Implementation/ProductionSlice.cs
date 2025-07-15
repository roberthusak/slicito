using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

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
        var originalLinkExplorer = originalSlice.GetLinkExplorer(linkType, sourceType, targetType);

        return new ProductionLinkExplorer(originalLinkExplorer, solutionContext);
    }

    public async ValueTask<IEnumerable<ElementInfo>> GetRootElementsAsync(ElementType? elementTypeFilter = null)
    {
        return await originalSlice.GetRootElementsAsync(elementTypeFilter);
    }

    private class ProductionLinkExplorer(ILazyLinkExplorer originalLinkExplorer, DotNetSolutionContext solutionContext) : ILazyLinkExplorer
    {
        public async ValueTask<IEnumerable<ElementInfo>> GetTargetElementsAsync(ElementId sourceId)
        {
            var originalElements = await originalLinkExplorer.GetTargetElementsAsync(sourceId);

            return originalElements.Where(e => !IsTestProject(e));
        }

        public async ValueTask<ElementInfo?> TryGetTargetElementAsync(ElementId sourceId)
        {
            var originalElement = await originalLinkExplorer.TryGetTargetElementAsync(sourceId);

            if (originalElement is null || IsTestProject(originalElement.Value))
            {
                return null;
            }

            return originalElement;
        }

        private bool IsTestProject(ElementInfo info)
        {
            if (!info.Type.Value.IsSubsetOfOrEquals(solutionContext.Types.Project.Value))
            {
                return false;
            }

            var project = solutionContext.GetProject(info);

            return project.Name.Contains("Tests");
        }
    }
}
