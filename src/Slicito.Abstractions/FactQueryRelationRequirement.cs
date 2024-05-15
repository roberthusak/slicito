namespace Slicito.Abstractions;

public sealed class FactQueryRelationRequirement(IRelationKind kind, bool includeChildless, Predicate<ILink>? filter = null)
{
    public IRelationKind Kind { get; } = kind;

    public bool IncludeChildless { get; } = includeChildless;

    public Predicate<ILink>? Filter { get; } = filter;
}
