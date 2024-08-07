using System.Collections.Immutable;

namespace Slicito.Abstractions;

public sealed record FactQuery(
    ImmutableArray<FactQueryElementRequirement> ElementRequirements,
    ImmutableArray<FactQueryRelationRequirement> RelationRequirements);
