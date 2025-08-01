namespace Slicito.Abstractions.Facts;

public interface IElement : IEquatable<IElement>
{
    ElementId Id { get; }
}
