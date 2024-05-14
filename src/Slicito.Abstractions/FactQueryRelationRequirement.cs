namespace Slicito.Abstractions;

public sealed class FactQueryRelationRequirement(IRelationKind kind, bool returnAll, Action<ILink>? filter = null)
{
    public IRelationKind Kind { get; } = kind;

    public bool ReturnAll { get; } = returnAll;

    public Action<ILink>? Filter { get; } = filter;
}
