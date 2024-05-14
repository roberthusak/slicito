namespace Slicito.Abstractions;

public interface ILink
{
    IElement Source { get; }

    IElement Target { get; }
}
