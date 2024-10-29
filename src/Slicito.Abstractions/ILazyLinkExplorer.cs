namespace Slicito.Abstractions;

public interface ILazyLinkExplorer
{
    ValueTask<ElementId> GetTargetElementIdAsync(ElementId sourceId);

    ValueTask<IEnumerable<ElementId>> GetTargetElementIdsAsync(ElementId sourceId);
}
