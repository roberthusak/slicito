using System.Collections.Immutable;

namespace Slicito.Abstractions;

public partial class Relation<TSourceElement, TTargetElement, TData> : IRelation<TSourceElement, TTargetElement, TData>
    where TSourceElement : class, IElement
    where TTargetElement : class, IElement
{
    private readonly Dictionary<TSourceElement, ImmutableArray<IPair<TSourceElement, TTargetElement, TData>>> _elementsToOutgoingMap;
    private readonly Dictionary<TTargetElement, ImmutableArray<IPair<TSourceElement, TTargetElement, TData>>> _elementsToIncomingMap;

    public IEnumerable<IPair<TSourceElement, TTargetElement, TData>> Pairs => _elementsToOutgoingMap.Values.SelectMany(l => l);

    private Relation(
        Dictionary<TSourceElement, List<IPair<TSourceElement, TTargetElement, TData>>> elementsToOutgoingMap,
        Dictionary<TTargetElement, List<IPair<TSourceElement, TTargetElement, TData>>> elementsToIncomingMap)
    {
        _elementsToOutgoingMap = new(elementsToOutgoingMap.Select(kvp =>
            KeyValuePair.Create(kvp.Key, kvp.Value.ToImmutableArray())));

        _elementsToIncomingMap = new(elementsToIncomingMap.Select(kvp =>
            KeyValuePair.Create(kvp.Key, kvp.Value.ToImmutableArray())));
    }

    public IEnumerable<TSourceElement> Sources => _elementsToOutgoingMap.Keys;

    public IEnumerable<TTargetElement> Targets => _elementsToIncomingMap.Keys;

    public IEnumerable<IPair<TSourceElement, TTargetElement, TData>> GetOutgoing(IElement source) =>
        GetPairsFromDictionary(_elementsToOutgoingMap, source);

    public IEnumerable<IPair<TSourceElement, TTargetElement, TData>> GetIncoming(IElement target) =>
        GetPairsFromDictionary(_elementsToIncomingMap, target);

    private IEnumerable<IPair<TSourceElement, TTargetElement, TData>>
        GetPairsFromDictionary<TElement>(
            Dictionary<TElement, ImmutableArray<IPair<TSourceElement, TTargetElement, TData>>> dictionary,
            IElement element)
        where TElement : class
    {
        if (element is not TElement typedElement
            || !dictionary.TryGetValue(typedElement, out var list))
        {
            return Enumerable.Empty<IPair<TSourceElement, TTargetElement, TData>>();
        }

        return list;
    }
}
