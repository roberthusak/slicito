using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Common.Implementation;

public abstract partial class ElementBase(ElementId id) : IElement
{
    public ElementId Id { get; } = id;
}
