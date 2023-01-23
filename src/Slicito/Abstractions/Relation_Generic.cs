using System.Collections.Immutable;

namespace Slicito.Abstractions;

public partial class Relation<TSourceElement, TTargetElement, TData> : IRelation<TSourceElement, TTargetElement, TData>
    where TSourceElement : class, IElement
    where TTargetElement : class, IElement
{
    public IEnumerable<IPair<TSourceElement, TTargetElement, TData>> Pairs { get; }

    private Relation(ImmutableArray<IPair<TSourceElement, TTargetElement, TData>> pairs)
    {
        Pairs = pairs;
    }
}
