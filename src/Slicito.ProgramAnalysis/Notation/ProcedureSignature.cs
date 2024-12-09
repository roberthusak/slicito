using System.Collections.Immutable;

namespace Slicito.ProgramAnalysis.Notation;

public record ProcedureSignature(
    string Name,
    ImmutableArray<DataType> ParameterTypes,
    ImmutableArray<DataType> ReturnTypes);
