using System;

namespace Slicito.Abstractions;

public interface IFactQueryElementRequirement
{
    IElementKind Kind { get; }

    bool ReturnAll { get; }

    Action<IElement>? Filter { get; }
}
