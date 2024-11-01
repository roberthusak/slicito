namespace Slicito.Abstractions.Queries;

public static class FactTypeExtensions
{
    public static bool IsStrictSupersetOf(this IFactType self, IFactType other) =>
        !self.Equals(other) && self.IsSupersetOfOrEquals(other);

    public static bool IsSupersetOfOrEquals(this IFactType self, IFactType other) =>
        self.GetSmallestCommonSuperset(other).Equals(self);

    public static bool IsStrictSubsetOf(this IFactType self, IFactType other) =>
        !self.Equals(other) && self.IsSubsetOfOrEquals(other);

    public static bool IsSubsetOfOrEquals(this IFactType self, IFactType other) =>
        other.IsSupersetOfOrEquals(self);

    public static IFactType GetUnionOrThrow(this IFactType self, IFactType other) =>
        self.TryGetUnion(other) ?? throw new InvalidOperationException(
            $"Cannot create the union of fact types {self} and {other}.");

    public static IFactType GetIntersectionOrThrow(this IFactType self, IFactType other) =>
        self.TryGetIntersection(other) ?? throw new InvalidOperationException(
            $"Cannot create the intersection of fact types {self} and {other}.");
}
