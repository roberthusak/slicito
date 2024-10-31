namespace Slicito.Abstractions.Queries;

public interface ITypeSystem
{
    IFactType GetFactType(IDictionary<string, IReadOnlyList<string>> attributeValues);
}
