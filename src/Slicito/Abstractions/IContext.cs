using Slicito.Abstractions.Relations;

namespace Slicito.Abstractions;

public interface IContext
{
    IEnumerable<IElement> Elements { get; }

    IBinaryRelation<IElement, IElement, EmptyStruct> Hierarchy { get; }
}
