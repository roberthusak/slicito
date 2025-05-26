using System.Diagnostics.CodeAnalysis;

namespace Slicito.Abstractions;

public interface ICache
{
    bool TryGet<TKey, TValue>(TKey key, [NotNullWhen(true)] out TValue? value)
        where TKey : notnull
        where TValue : notnull;

    void Set<TKey, TValue>(TKey key, TValue value)
        where TKey : notnull
        where TValue : notnull;
}
