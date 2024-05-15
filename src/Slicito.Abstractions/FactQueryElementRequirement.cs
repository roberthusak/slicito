namespace Slicito.Abstractions;

public sealed class FactQueryElementRequirement(IElementKind kind, bool includeChildless, Predicate<IElement>? filter = null)
{
    public IElementKind Kind { get; } = kind;

    public bool IncludeChildless { get; } = includeChildless;

    public Predicate<IElement>? Filter { get; } = filter;
}
