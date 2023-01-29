namespace Slicito.Abstractions;

public partial class Relation<TSourceElement, TTargetElement, TData>
{
    public class Builder : IRelation<TSourceElement, TTargetElement, TData>
    {
        private readonly Dictionary<TSourceElement, List<IPair<TSourceElement, TTargetElement, TData>>> _elementsToOutgoingMap = new();
        private readonly Dictionary<TTargetElement, List<IPair<TSourceElement, TTargetElement, TData>>> _elementsToIncomingMap = new();

        public IEnumerable<IPair<TSourceElement, TTargetElement, TData>> Pairs => _elementsToOutgoingMap.Values.SelectMany(l => l);

        public IRelation<TSourceElement, TTargetElement, TData> AsRelation() => this;

        public Builder Add(TSourceElement source, TTargetElement target, TData data) =>
            Add(Pair.Create(source, target, data));

        public Builder Add(IPair<TSourceElement, TTargetElement, TData> pair)
        {
            GetOrCreateList(_elementsToOutgoingMap, pair.Source).Add(pair);
            GetOrCreateList(_elementsToIncomingMap, pair.Target).Add(pair);

            return this;

            static List<IPair<TSourceElement, TTargetElement, TData>>
                GetOrCreateList<TElement>(
                    Dictionary<TElement, List<IPair<TSourceElement, TTargetElement, TData>>> dictionary,
                    TElement element)
                where TElement : notnull
            {
                if (dictionary.TryGetValue(element, out var list))
                {
                    return list;
                }
                else
                {
                    list = new();
                    dictionary[element] = list;

                    return list;
                }
            }
        }

        public Builder AddRange(IEnumerable<IPair<TSourceElement, TTargetElement, TData>> pairs)
        {
            foreach (var pair in pairs)
            {
                Add(pair);
            }

            return this;
        }

        public Relation<TSourceElement, TTargetElement, TData> Build() =>
            new(_elementsToOutgoingMap, _elementsToIncomingMap);
    }
}
