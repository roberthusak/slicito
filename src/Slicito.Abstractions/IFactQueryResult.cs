using System.Collections.Generic;

namespace Slicito.Abstractions;

public interface IFactQueryResult
{
    IEnumerable<IElement> Elements { get; }

    IEnumerable<IRelation> Relations { get; }
}
