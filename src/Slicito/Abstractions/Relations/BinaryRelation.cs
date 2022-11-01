using System.Collections.Immutable;

namespace Slicito.Abstractions.Relations;

public class BinaryRelation<TSourceElement, TTargetElement, TData> : IBinaryRelation<TSourceElement, TTargetElement, TData>
    where TSourceElement : class, IElement
    where TTargetElement : class, IElement
{
    public IEnumerable<IPair<TSourceElement, TTargetElement, TData>> Pairs { get; }

    private BinaryRelation(ImmutableArray<IPair<TSourceElement, TTargetElement, TData>> pairs)
    {
        Pairs = pairs;
    }

    public class Builder : IBinaryRelation<TSourceElement, TTargetElement, TData>
    {
        private readonly List<IPair<TSourceElement, TTargetElement, TData>> _pairs = new();

        public IEnumerable<IPair<TSourceElement, TTargetElement, TData>> Pairs => _pairs;

        public Builder Add(TSourceElement source, TTargetElement target, TData data)
        {
            _pairs.Add(new Pair<TSourceElement, TTargetElement, TData>(source, target, data));

            return this;
        }

        public Builder Add(IPair<TSourceElement, TTargetElement, TData> pair)
        {
            _pairs.Add(pair);

            return this;
        }

        public Builder AddRange(IEnumerable<IPair<TSourceElement, TTargetElement, TData>> pairs)
        {
            _pairs.AddRange(pairs);

            return this;
        }

        public BinaryRelation<TSourceElement, TTargetElement, TData> Build() =>
            new(_pairs.ToImmutableArray());
    }
}
