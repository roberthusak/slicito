using Slicito.Abstractions;

namespace Slicito.Presentation;

public interface IUriProvider
{
    Uri? TryGetUriForElement(IElement element);

    Uri? TryGetUriForPair(object pair);
}
