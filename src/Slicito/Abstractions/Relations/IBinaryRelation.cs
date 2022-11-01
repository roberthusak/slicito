namespace Slicito.Abstractions.Relations;

public interface IBinaryRelation<out TSourceElement, out TTargetElement, out TData>
    where TSourceElement : class, IElement
    where TTargetElement : class, IElement
{
    IEnumerable<IPair<TSourceElement, TTargetElement, TData>> Pairs { get; }

    IEnumerable<TSourceElement> Sources =>
        Pairs.Select(x => x.Source);

    IEnumerable<TTargetElement> Targets =>
        Pairs.Select(x => x.Target);

    IEnumerable<IPair<TSourceElement, TTargetElement, TData>> GetOutgoing(IElement source) =>
        Pairs.Where(d => d.Source == source);

    IEnumerable<IPair<TSourceElement, TTargetElement, TData>> GetOutgoing(IEnumerable<IElement> sources) =>
        sources.SelectMany(source => GetOutgoing(source));

    IEnumerable<IPair<TSourceElement, TTargetElement, TData>> GetIncoming(IElement target) =>
        Pairs.Where(d => d.Target == target);

    IEnumerable<IPair<TSourceElement, TTargetElement, TData>> GetIncoming(IEnumerable<IElement> targets) =>
        targets.SelectMany(target => GetIncoming(target));
}
