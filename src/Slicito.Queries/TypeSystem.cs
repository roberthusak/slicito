using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Web;

using Slicito.Abstractions.Queries;

namespace Slicito.Queries;

public class TypeSystem : ITypeSystem
{
    private readonly ConcurrentDictionary<string, FactType> _factTypes = new();

    public IFactType GetFactType(IDictionary<string, IEnumerable<string>> attributeValues)
    {
        var immutableAttributeValues = attributeValues.ToImmutableSortedDictionary(
            kv => kv.Key,
            kv => (IEnumerable<string>)kv.Value.ToImmutableSortedSet());
        var uniqueSerialization = GetUniqueSerialization(immutableAttributeValues);

        return _factTypes.GetOrAdd(
            uniqueSerialization,
            _ => new FactType(this, immutableAttributeValues, uniqueSerialization));
    }

    private string GetUniqueSerialization(ImmutableSortedDictionary<string, IEnumerable<string>> attributeValues) =>
        string.Join(
            ";",
            attributeValues
                .OrderBy(kv => HttpUtility.UrlEncode(kv.Key))
                .Select(kv => $"{kv.Key}={string.Join(",", kv.Value.Select(HttpUtility.UrlEncode))}"));
}
