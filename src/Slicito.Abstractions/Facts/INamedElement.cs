namespace Slicito.Abstractions.Facts;

public interface INamedElement : IElement
{
    string Name { get; }
}
