namespace Slicito.Abstractions;

public sealed class FactQueryElementRequirement(IElementKind kind, bool returnAll, Action<IElement>? filter = null)
{
    public IElementKind Kind { get; } = kind;

    public bool ReturnAll { get; } = returnAll;

    public Action<IElement>? Filter { get; } = filter;
}
