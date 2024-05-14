namespace Slicito.Abstractions;

public sealed class FactQueryResult(IEnumerable<IElement> elements, IEnumerable<IRelation> relations)
{
    public IEnumerable<IElement> Elements { get; } = elements;

    public IEnumerable<IRelation> Relations { get; } = relations;
}
