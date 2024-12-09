namespace Slicito.Abstractions.Queries;

public interface IFactType : IEquatable<IFactType>
{
    IReadOnlyDictionary<string, IReadOnlyList<string>> AttributeValues { get; }

    IFactType GetSmallestCommonSuperset(IFactType other);

    IFactType? TryGetUnion(IFactType other);

    IFactType? TryGetIntersection(IFactType other);
}
