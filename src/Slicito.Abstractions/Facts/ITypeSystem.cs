namespace Slicito.Abstractions.Facts;

public interface ITypeSystem
{
    IFactType GetFactType(IDictionary<string, IReadOnlyList<string>> attributeValues);
}
