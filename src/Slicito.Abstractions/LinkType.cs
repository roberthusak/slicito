using Slicito.Abstractions.Queries;

namespace Slicito.Abstractions;

public record struct LinkType(IFactType Value)
{
    public static LinkType operator |(LinkType left, LinkType right)
    {
        return new(left.Value.GetUnionOrThrow(right.Value));
    }

    public static LinkType operator &(LinkType left, LinkType right)
    {
        return new(left.Value.GetIntersectionOrThrow(right.Value));
    }
}
