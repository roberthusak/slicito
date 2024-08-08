namespace Slicito.Abstractions.Queries;

public static class ElementTypeExtensions
{
    public static bool IsSupersetOfOrEquals(this IElementType self, IElementType other) =>
        self.GetSmallestCommonSuperset(other).Equals(self);

    public static bool IsSubsetOfOrEquals(this IElementType self, IElementType other) =>
        other.IsSupersetOfOrEquals(self);
}
