using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Web;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Abstractions.Facts.Attributes;
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

    public ElementType GetElementTypeFromInterface(Type interfaceType)
    {
        if (!interfaceType.IsInterface)
        {
            throw new ArgumentException("Type must be an interface", nameof(interfaceType));
        }

        if (interfaceType == typeof(IElement))
        {
            return this.GetUnrestrictedElementType();
        }

        var attributes = new Dictionary<string, IReadOnlyList<string>>();
        
        var elementAttributes = interfaceType.GetCustomAttributes(typeof(ElementAttributeAttribute), true)
            .Cast<ElementAttributeAttribute>();
        foreach (var attr in elementAttributes)
        {
            if (!attributes.ContainsKey(attr.Name))
            {
                attributes[attr.Name] = [];
            }
            attributes[attr.Name] = [.. attributes[attr.Name], attr.Value];
        }

        foreach (var baseInterface in interfaceType.GetInterfaces())
        {
            var baseElementType = GetElementTypeFromInterface(baseInterface);
            var baseAttributes = baseElementType.Value.AttributeValues;
            
            foreach (var kvp in baseAttributes)
            {
                if (!attributes.ContainsKey(kvp.Key))
                {
                    attributes[kvp.Key] = [];
                }
                attributes[kvp.Key] = [.. attributes[kvp.Key], .. kvp.Value];
            }
        }

        return this.GetElementType(attributes.SelectMany(kv => kv.Value.Select(v => (kv.Key, v))));
    }
}
