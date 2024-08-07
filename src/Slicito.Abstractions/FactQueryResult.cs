namespace Slicito.Abstractions;

public sealed record FactQueryResult(IEnumerable<IElement> Elements, IEnumerable<IRelation> Relations);
