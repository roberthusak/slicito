namespace Slicito.Abstractions;

public interface ILazyLinkExplorer
{
    ValueTask<ElementInfo?> TryGetTargetElementAsync(ElementId sourceId);

    ValueTask<IEnumerable<ElementInfo>> GetTargetElementsAsync(ElementId sourceId);
}
