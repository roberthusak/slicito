using Slicito.Abstractions;

namespace Slicito.Queries;

internal class LazyLinkExplorer : ILazyLinkExplorer
{
    public ValueTask<ElementId> GetTargetElementIdAsync(ElementId sourceId)
    {
        throw new NotImplementedException();
    }

    public ValueTask<IEnumerable<ElementId>> GetTargetElementIdsAsync(ElementId sourceId)
    {
        throw new NotImplementedException();
    }
}
