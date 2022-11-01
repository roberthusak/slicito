using Slicito.Abstractions.Relations;

namespace Slicito.Abstractions;

public interface IContext<TBaseElement, THierarchyDetail>
    where TBaseElement : class, IElement
{
    IEnumerable<TBaseElement> Elements { get; }

    IBinaryRelation<TBaseElement, TBaseElement, THierarchyDetail> Hierarchy { get; }
}
