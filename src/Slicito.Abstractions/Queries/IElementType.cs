namespace Slicito.Abstractions.Queries;

public interface IElementType : IEquatable<IElementType>
{
    IReadOnlyDictionary<string, IEnumerable<string>> AttributeValues { get; }

    IElementType GetSmallestCommonSuperset(IElementType other);

    IElementType? TryGetUnion(IElementType other);

    IElementType? TryGetIntersection(IElementType other);
}
