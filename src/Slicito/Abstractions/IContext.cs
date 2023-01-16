using Slicito.Abstractions.Relations;
using Slicito.Presentation;

namespace Slicito.Abstractions;

public interface IContext
{
    IEnumerable<IElement> Elements { get; }

    IBinaryRelation<IElement, IElement, EmptyStruct> Hierarchy { get; }

    ILabelProvider LabelProvider { get; }

    IUriProvider GetOpenInIdeUriProvider(GetUriDelegate? getUriDelegate);
}
