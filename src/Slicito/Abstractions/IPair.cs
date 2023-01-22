namespace Slicito.Abstractions;

public interface IPair<out TSourceElement, out TTargetElement, out TData>
    where TSourceElement : class, IElement
    where TTargetElement : class, IElement
{
    public TSourceElement Source { get; }

    public TTargetElement Target { get; }

    public TData Data { get; }
}
