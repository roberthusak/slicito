namespace Slicito.Abstractions;

public sealed record FactQueryElementRequirement(IElementKind Kind, bool IncludeChildless, Predicate<IElement>? Filter = null);
