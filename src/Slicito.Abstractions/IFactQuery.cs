using System.Collections.Immutable;

namespace Slicito.Abstractions;

public interface IFactQuery
{
    ImmutableArray<IFactQueryElementRequirement> ElementRequirements { get; }

    ImmutableArray<IFactQueryRelationRequirement> RelationRequirements { get; }
}
