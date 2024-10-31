using Slicito.Abstractions.Queries;

namespace Slicito.Abstractions;

public record struct ElementType(IFactType Value)
{
    public static ElementType? operator |(ElementType left, ElementType right)
    {
        return left.Value.TryGetUnion(right.Value) is { } union ? new(union) : null;
    }
}
