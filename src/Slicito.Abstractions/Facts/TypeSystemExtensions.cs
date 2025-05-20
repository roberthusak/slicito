namespace Slicito.Abstractions.Facts;

public static class TypeSystemExtensions
{
    public static IFactType GetFactType(
        this ITypeSystem typeSystem,
        IEnumerable<(string name, string value)> attributeValues)
    {
        var attributeDictionary = attributeValues
            .ToDictionary(
                attribute => attribute.name,
                attribute => (IReadOnlyList<string>) [attribute.value]);

        return typeSystem.GetFactType(attributeDictionary);
    }

    public static IFactType GetUnrestrictedFactType(this ITypeSystem typeSystem) =>
        typeSystem.GetFactType(Enumerable.Empty<(string name, string value)>());

    public static IFactType GetFactType(this ITypeSystem typeSystem,
        IEnumerable<(string name, IEnumerable<string> values)> attributeValues)
    {
        var attributeDictionary = attributeValues
            .ToDictionary(
                attribute => attribute.name,
                attribute => (IReadOnlyList<string>) [.. attribute.values]);

        return typeSystem.GetFactType(attributeDictionary);
    }

    public static ElementType GetElementType(
        this ITypeSystem typeSystem,
        IEnumerable<(string name, string value)> attributeValues)
    {
        return new(typeSystem.GetFactType(attributeValues));
    }

    public static ElementType GetElementType(
        this ITypeSystem typeSystem,
        IEnumerable<(string name, IEnumerable<string> values)> attributeValues)
    {
        return new(typeSystem.GetFactType(attributeValues));
    }

    public static ElementType GetUnrestrictedElementType(this ITypeSystem typeSystem) =>
        new(typeSystem.GetUnrestrictedFactType());

    public static LinkType GetLinkType(
        this ITypeSystem typeSystem,
        IEnumerable<(string name, string value)> attributeValues)
    {
        return new(typeSystem.GetFactType(attributeValues));
    }

    public static LinkType GetLinkType(
        this ITypeSystem typeSystem,
        IEnumerable<(string name, IEnumerable<string> values)> attributeValues)
    {
        return new(typeSystem.GetFactType(attributeValues));
    }

    public static LinkType GetUnrestrictedLinkType(this ITypeSystem typeSystem) =>
        new(typeSystem.GetUnrestrictedFactType());
}
