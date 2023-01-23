using Slicito.Presentation;

namespace Slicito.Abstractions;

public interface IContext
{
    IEnumerable<IElement> Elements { get; }

    IRelation<IElement, IElement, EmptyStruct> Hierarchy { get; }

    ILabelProvider LabelProvider { get; }

    IUriProvider GetOpenInIdeUriProvider(GetUriDelegate? getUriDelegate);
}
