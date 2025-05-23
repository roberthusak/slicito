using System.Collections.Immutable;

namespace Slicito.Common.Collections;

public readonly struct ContentEquatableArray<T> : IEquatable<ContentEquatableArray<T>>
{
    public ImmutableArray<T> Array { get; }

    public ContentEquatableArray(ImmutableArray<T> array)
    {
        Array = array;
    }

    public ContentEquatableArray(IEnumerable<T> items)
    {
        Array = [.. items];
    }

    public static implicit operator ImmutableArray<T>(ContentEquatableArray<T> array)
    {
        return array.Array;
    }

    public static implicit operator ContentEquatableArray<T>(ImmutableArray<T> array)
    {
        return new(array);
    }

    public static bool operator ==(ContentEquatableArray<T> left, ContentEquatableArray<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ContentEquatableArray<T> left, ContentEquatableArray<T> right)
    {
        return !left.Equals(right);
    }

    public override bool Equals(object? obj)
    {
        if (obj is ContentEquatableArray<T> other)
        {
            return Equals(other);
        }
        return false;
    }

    public bool Equals(ContentEquatableArray<T> other)
    {
        if (Array.Length != other.Array.Length)
        {
            return false;
        }

        return Array.SequenceEqual(other.Array);
    }

    public override int GetHashCode()
    {
        var hashCode = 0;
        foreach (var item in Array)
        {
            hashCode = HashCode.Combine(hashCode, item?.GetHashCode() ?? 0);
        }
        return hashCode;
    }
}
