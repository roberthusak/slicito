namespace Slicito.Abstractions.Queries;

public interface IFactType : IEquatable<IFactType>
{
    IReadOnlyDictionary<string, IEnumerable<string>> AttributeValues { get; }

    IFactType GetSmallestCommonSuperset(IFactType other);

    IFactType? TryGetUnion(IFactType other);

    IFactType? TryGetIntersection(IFactType other);
}
