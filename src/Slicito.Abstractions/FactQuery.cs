using System.Collections.Immutable;

namespace Slicito.Abstractions;

public sealed class FactQuery(
    ImmutableArray<FactQueryElementRequirement> elementRequirements,
    ImmutableArray<FactQueryRelationRequirement> relationRequirements)
{
    public ImmutableArray<FactQueryElementRequirement> ElementRequirements { get; } = elementRequirements;

    public ImmutableArray<FactQueryRelationRequirement> RelationRequirements { get; } = relationRequirements;
}
