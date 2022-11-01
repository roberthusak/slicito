namespace Slicito.Abstractions.Relations;

public record Pair<TSourceElement, TTargetElement, TData>(TSourceElement Source, TTargetElement Target, TData Data)
    : IPair<TSourceElement, TTargetElement, TData>
    where TSourceElement: class, IElement
    where TTargetElement: class, IElement;

public static class Pair
{
    public static Pair<TSourceElement, TTargetElement, TData>
        Create<TSourceElement, TTargetElement, TData>(TSourceElement source, TTargetElement target, TData data)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    =>
        new Pair<TSourceElement, TTargetElement, TData>(source, target, data);
}
