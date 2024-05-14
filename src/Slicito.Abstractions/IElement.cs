namespace Slicito.Abstractions;

public interface IElement
{
    IElementKind Kind { get; }

    string Id { get; }
}
