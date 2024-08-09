namespace Slicito.Abstractions.Queries;

public static class FactTypeExtensions
{
    public static bool IsSupersetOfOrEquals(this IFactType self, IFactType other) =>
        self.GetSmallestCommonSuperset(other).Equals(self);

    public static bool IsSubsetOfOrEquals(this IFactType self, IFactType other) =>
        other.IsSupersetOfOrEquals(self);
}
