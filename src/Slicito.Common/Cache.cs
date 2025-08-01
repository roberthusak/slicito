using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.Common;

public class Cache : ICache
{
    private readonly ConcurrentDictionary<object, object> _cache = new();

    public bool TryGet<TKey, TValue>(TKey key, [NotNullWhen(true)] out TValue? value)
        where TKey : notnull
        where TValue : notnull
    {
        if (_cache.TryGetValue(key, out var objectValue))
        {
            value = (TValue)objectValue;
            return true;
        }
        value = default;
        return false;
    }

    public void Set<TKey, TValue>(TKey key, TValue value)
        where TKey : notnull
        where TValue : notnull
    {
        _cache.AddOrUpdate(key, value, (_, _) => value);
    }
}

