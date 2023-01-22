using System.Collections.Immutable;

namespace Slicito.Abstractions;

public partial class BinaryRelation<TSourceElement, TTargetElement, TData> : IBinaryRelation<TSourceElement, TTargetElement, TData>
    where TSourceElement : class, IElement
    where TTargetElement : class, IElement
{
    public IEnumerable<IPair<TSourceElement, TTargetElement, TData>> Pairs { get; }

    private BinaryRelation(ImmutableArray<IPair<TSourceElement, TTargetElement, TData>> pairs)
    {
        Pairs = pairs;
    }
}
