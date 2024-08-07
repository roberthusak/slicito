namespace Slicito.Abstractions;

public sealed record FactQueryRelationRequirement(IRelationKind Kind, bool IncludeChildless, Predicate<ILink>? Filter = null);
