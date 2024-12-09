using System.Collections.Immutable;
using System.Diagnostics;

using Slicito.Abstractions.Queries;
using Slicito.Common;

namespace Slicito.Common.Implementation;

internal class FactType : IFactType
{
    private readonly TypeSystem _typeSystem;
    private readonly ImmutableSortedDictionary<string, IReadOnlyList<string>> _attributeValues;     // The values are ImmutableSortedSets.
    private readonly string _uniqueSerialization;

    public FactType(TypeSystem typeSystem, ImmutableSortedDictionary<string, IReadOnlyList<string>> attributeValues, string uniqueSerialization)
    {
        _typeSystem = typeSystem;
        _attributeValues = attributeValues;
        _uniqueSerialization = uniqueSerialization;
    }

    public IReadOnlyDictionary<string, IReadOnlyList<string>> AttributeValues => _attributeValues;

    public bool Equals(IFactType other)
    {
        Debug.Assert(other is not null);

        if (other is not FactType otherFactType || _typeSystem != otherFactType._typeSystem)
        {
            throw new InvalidOperationException("Cannot compare fact types from different type systems.");
        }

        // The type system ensures that there's only one instance of each fact type.
        return ReferenceEquals(this, other);
    }

    public IFactType GetSmallestCommonSuperset(IFactType other)
    {
        if (other is not FactType otherFactType || _typeSystem != otherFactType._typeSystem)
        {
            throw new InvalidOperationException("Cannot combine fact types from different type systems.");
        }

        if (Equals(otherFactType))
        {
            return this;
        }

        var keys = _attributeValues.Keys.Intersect(otherFactType._attributeValues.Keys);

        var attributeValues = ImmutableSortedDictionary.CreateRange(
            keys.Select(key =>
            {
                return new KeyValuePair<string, IReadOnlyList<string>>(
                    key,
                    [.. _attributeValues[key].Union(otherFactType._attributeValues[key])]);
            }));

        return _typeSystem.GetFactType(attributeValues);
    }

    public IFactType? TryGetUnion(IFactType other)
    {
        if (other is not FactType otherFactType || _typeSystem != otherFactType._typeSystem)
        {
            throw new InvalidOperationException("Cannot combine fact types from different type systems.");
        }

        if (Equals(otherFactType))
        {
            return this;
        }

        if (!_attributeValues.Keys.SequenceEqual(otherFactType._attributeValues.Keys))
        {
            // The only attempt at creating a union would be to leave certain keys.
            // This would inadverently include facts not present in either type.
            return null;
        }

        var keysWithDifferentValues =
            _attributeValues.Keys
            .Where(key => !_attributeValues[key].SequenceEqual(otherFactType._attributeValues[key]))
            .ToArray();
        if (keysWithDifferentValues.Length != 1)
        {
            // The types can't be equal due to the previous check.
            Debug.Assert(keysWithDifferentValues.Length != 0);

            // If there's more than one key with different values, the attempt at creating a union
            // would include their unexpected combinations.
            return null;
        }

        var keyToMerge = keysWithDifferentValues.Single();

        var attributeValues = _attributeValues.SetItem(
            keyToMerge,
            [.. _attributeValues[keyToMerge].Union(otherFactType._attributeValues[keyToMerge])]);

        return _typeSystem.GetFactType(attributeValues);
    }

    public IFactType? TryGetIntersection(IFactType other)
    {
        if (other is not FactType otherFactType || _typeSystem != otherFactType._typeSystem)
        {
            throw new InvalidOperationException("Cannot combine fact types from different type systems.");
        }

        if (Equals(otherFactType))
        {
            return this;
        }

        var keys = _attributeValues.Keys.Union(otherFactType._attributeValues.Keys);

        var attributeValues = ImmutableSortedDictionary.CreateRange(
            keys.Select(key =>
            {
                IEnumerable<string> values;
                if (!_attributeValues.TryGetValue(key, out var values1))
                {
                    values = otherFactType._attributeValues[key];
                }
                else if (!otherFactType._attributeValues.TryGetValue(key, out var values2))
                {
                    values = _attributeValues[key];
                }
                else
                {
                    values = values1.Intersect(values2);
                }

                return new KeyValuePair<string, IReadOnlyList<string>>(key, [.. values]);
            }));

        if (attributeValues.Values.Any(values => !values.Any()))
        {
            // The type system doesn't support a type that can't include any facts.
            return null;
        }

        return _typeSystem.GetFactType(attributeValues);
    }

    public override string ToString() => $"FactType: {_uniqueSerialization}";
}
