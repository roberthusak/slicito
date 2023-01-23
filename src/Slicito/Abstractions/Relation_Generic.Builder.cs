using System.Collections.Immutable;

namespace Slicito.Abstractions;

public partial class Relation<TSourceElement, TTargetElement, TData>
{
    public class Builder : IRelation<TSourceElement, TTargetElement, TData>
    {
        private readonly List<IPair<TSourceElement, TTargetElement, TData>> _pairs = new();

        public IEnumerable<IPair<TSourceElement, TTargetElement, TData>> Pairs => _pairs;

        public IRelation<TSourceElement, TTargetElement, TData> AsRelation() => this;

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

        public Relation<TSourceElement, TTargetElement, TData> Build() =>
            new(_pairs.ToImmutableArray());
    }
}
