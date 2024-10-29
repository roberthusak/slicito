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
}
