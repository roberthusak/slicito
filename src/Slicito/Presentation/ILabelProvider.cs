using Slicito.Abstractions;

namespace Slicito.Presentation;

public interface ILabelProvider
{
    string? TryGetLabelForElement(IElement element, IElement? containingElement);

    string? TryGetLabelForPair(object pair);
}
