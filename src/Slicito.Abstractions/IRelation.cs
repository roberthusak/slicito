using System.Collections.Generic;

namespace Slicito.Abstractions;

public interface IRelation
{
    IRelationKind Kind { get; }

    IEnumerable<ILink> Links { get; }
}
