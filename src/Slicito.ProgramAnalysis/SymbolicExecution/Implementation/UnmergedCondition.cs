using Slicito.ProgramAnalysis.Notation;
using Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

using VersionMap = System.Collections.Immutable.ImmutableDictionary<Slicito.ProgramAnalysis.SymbolicExecution.SmtLib.Function.Nullary, int>;

namespace Slicito.ProgramAnalysis.SymbolicExecution.Implementation;

internal record UnmergedCondition(BasicBlock PreviousBlock, Term Condition, VersionMap VersionMap);
