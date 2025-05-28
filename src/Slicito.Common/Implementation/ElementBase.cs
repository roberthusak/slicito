using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Common.Implementation;

public abstract partial class ElementBase(ElementId id) : IElement
{
    public ElementId Id { get; } = id;

    public bool Equals(IElement other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is IElement element && Equals(element);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}
