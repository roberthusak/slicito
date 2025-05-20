using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Web;

using Slicito.Abstractions.Facts;
using Slicito.Common.Implementation;

namespace Slicito.Common;

public class TypeSystem : ITypeSystem
{
    private readonly ConcurrentDictionary<string, FactType> _factTypes = new();

    public IFactType GetFactType(IDictionary<string, IReadOnlyList<string>> attributeValues)
    {
        var immutableAttributeValues = attributeValues.ToImmutableSortedDictionary(
            kv => kv.Key,
            kv => (IReadOnlyList<string>) kv.Value.ToImmutableSortedSet());
        var uniqueSerialization = GetUniqueSerialization(immutableAttributeValues);

        return _factTypes.GetOrAdd(
            uniqueSerialization,
            _ => new FactType(this, immutableAttributeValues, uniqueSerialization));
    }

    private string GetUniqueSerialization(ImmutableSortedDictionary<string, IReadOnlyList<string>> attributeValues) =>
        string.Join(
            ";",
            attributeValues
                .OrderBy(kv => kv.Key)
                .Select(kv => $"{HttpUtility.UrlEncode(kv.Key)}={string.Join(",", kv.Value.Select(HttpUtility.UrlEncode))}"));
}
