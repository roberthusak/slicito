using Slicito.Abstractions.Queries;

namespace Slicito.Abstractions;

public record struct ElementType(IFactType Value)
{
    public static ElementType operator |(ElementType left, ElementType right)
    {
        return new(left.Value.GetUnionOrThrow(right.Value));
    }

    public static ElementType operator &(ElementType left, ElementType right)
    {
        return new(left.Value.GetIntersectionOrThrow(right.Value));
    }
}
