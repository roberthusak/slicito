using Slicito.Abstractions.Queries;

namespace Slicito.Abstractions;

public record struct LinkType(IFactType Value)
{
    public static LinkType? operator |(LinkType left, LinkType right)
    {
        return left.Value.TryGetUnion(right.Value) is { } union ? new(union) : null;
    }
}
