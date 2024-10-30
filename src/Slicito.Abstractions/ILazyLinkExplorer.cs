namespace Slicito.Abstractions;

public interface ILazyLinkExplorer
{
    ValueTask<ElementId?> TryGetTargetElementIdAsync(ElementId sourceId);

    ValueTask<IEnumerable<ElementId>> GetTargetElementIdsAsync(ElementId sourceId);
}
