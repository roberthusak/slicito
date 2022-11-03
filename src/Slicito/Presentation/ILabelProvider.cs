using Slicito.Abstractions;

namespace Slicito.Presentation;

public interface ILabelProvider
{
    string? TryGetLabelForElement(IElement element);

    string? TryGetLabelForPair(object pair);
}
