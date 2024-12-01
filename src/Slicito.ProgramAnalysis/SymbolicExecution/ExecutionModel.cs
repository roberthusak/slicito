using System.Collections.Immutable;

using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis.SymbolicExecution;

public record ExecutionModel(ImmutableArray<Expression.Constant> ParameterValues);
