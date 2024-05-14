namespace Slicito.Abstractions;

public sealed class FactQueryRelationRequirement(IRelationKind kind, bool returnAll, Predicate<ILink>? filter = null)
{
    public IRelationKind Kind { get; } = kind;

    public bool ReturnAll { get; } = returnAll;

    public Predicate<ILink>? Filter { get; } = filter;
}
