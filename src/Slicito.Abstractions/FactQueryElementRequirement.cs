namespace Slicito.Abstractions;

public sealed class FactQueryElementRequirement(IElementKind kind, bool returnAll, Predicate<IElement>? filter = null)
{
    public IElementKind Kind { get; } = kind;

    public bool ReturnAll { get; } = returnAll;

    public Predicate<IElement>? Filter { get; } = filter;
}
