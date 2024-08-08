namespace Slicito.Abstractions.Queries;

public interface ITypeSystem
{
    IElementType GetElementType(IDictionary<string, IEnumerable<string>> attributeValues);
}
