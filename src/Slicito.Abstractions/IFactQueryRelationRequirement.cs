using System;

namespace Slicito.Abstractions;

public interface IFactQueryRelationRequirement
{
    IRelationKind Kind { get; }

    bool ReturnAll { get; }

    Action<ILink>? Filter { get; }
}
