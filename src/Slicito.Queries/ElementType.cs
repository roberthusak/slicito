using System.Collections.Immutable;
using System.Diagnostics;

using Slicito.Abstractions.Queries;

namespace Slicito.Queries;

internal class ElementType : IElementType
{
    private readonly TypeSystem _typeSystem;
    private readonly ImmutableSortedDictionary<string, IEnumerable<string>> _attributeValues;     // The values are ImmutableSortedSets.
    private readonly string _uniqueSerialization;

    public ElementType(TypeSystem typeSystem, ImmutableSortedDictionary<string, IEnumerable<string>> attributeValues, string uniqueSerialization)
    {
        _typeSystem = typeSystem;
        _attributeValues = attributeValues;
        _uniqueSerialization = uniqueSerialization;
    }

    public IReadOnlyDictionary<string, IEnumerable<string>> AttributeValues => _attributeValues;

    public bool Equals(IElementType other)
    {
        Debug.Assert(other is not null);

        if (other is not ElementType otherElementType || _typeSystem != otherElementType._typeSystem)
        {
            throw new InvalidOperationException("Cannot compare element types from different type systems.");
        }

        // The type system ensures that there's only one instance of each element type.
        return ReferenceEquals(this, other);
    }

    public IElementType GetSmallestCommonSuperset(IElementType other)
    {
        if (other is not ElementType otherElementType || _typeSystem != otherElementType._typeSystem)
        {
            throw new InvalidOperationException("Cannot combine element types from different type systems.");
        }

        if (Equals(otherElementType))
        {
            return this;
        }

        var keys = _attributeValues.Keys.Intersect(otherElementType._attributeValues.Keys);

        var attributeValues = ImmutableSortedDictionary.CreateRange(
            keys.Select(key =>
            {
                var values = _attributeValues[key].Union(otherElementType._attributeValues[key]);
                return new KeyValuePair<string, IEnumerable<string>>(key, values);
            }));

        return _typeSystem.GetElementType(attributeValues);
    }

    public IElementType? TryGetUnion(IElementType other)
    {
        if (other is not ElementType otherElementType || _typeSystem != otherElementType._typeSystem)
        {
            throw new InvalidOperationException("Cannot combine element types from different type systems.");
        }

        if (Equals(otherElementType))
        {
            return this;
        }

        if (!_attributeValues.Keys.SequenceEqual(otherElementType._attributeValues.Keys))
        {
            // The only attempt at creating a union would be to leave certain keys.
            // This would inadverently include elements not present in either type.
            return null;
        }

        var keysWithDifferentValues =
            _attributeValues.Keys
            .Where(key => !_attributeValues[key].SequenceEqual(otherElementType._attributeValues[key]))
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
            _attributeValues[keyToMerge].Union(otherElementType._attributeValues[keyToMerge]));

        return _typeSystem.GetElementType(attributeValues);
    }

    public IElementType? TryGetIntersection(IElementType other)
    {
        if (other is not ElementType otherElementType || _typeSystem != otherElementType._typeSystem)
        {
            throw new InvalidOperationException("Cannot combine element types from different type systems.");
        }

        if (Equals(otherElementType))
        {
            return this;
        }

        var keys = _attributeValues.Keys.Union(otherElementType._attributeValues.Keys);

        var attributeValues = ImmutableSortedDictionary.CreateRange(
            keys.Select(key =>
            {
                IEnumerable<string> values;
                if (!_attributeValues.TryGetValue(key, out var values1))
                {
                    values = otherElementType._attributeValues[key];
                }
                else if (!otherElementType._attributeValues.TryGetValue(key, out var values2))
                {
                    values = _attributeValues[key];
                }
                else
                {
                    values = values1.Intersect(values2);
                }

                return new KeyValuePair<string, IEnumerable<string>>(key, values);
            }));

        if (attributeValues.Values.Any(values => !values.Any()))
        {
            // The type system doesn't support a type that can't include any elements.
            return null;
        }

        return _typeSystem.GetElementType(attributeValues);
    }

    public override string ToString() => $"ElementType: {_uniqueSerialization}";
}
