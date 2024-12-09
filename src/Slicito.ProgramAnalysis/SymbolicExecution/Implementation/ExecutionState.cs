using Slicito.ProgramAnalysis.Notation;
using Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

using VersionMap = System.Collections.Immutable.ImmutableDictionary<Slicito.ProgramAnalysis.SymbolicExecution.SmtLib.Function.Nullary, int>;

namespace Slicito.ProgramAnalysis.SymbolicExecution.Implementation;

internal record ExecutionState(
    BasicBlock CurrentBlock,
    VersionMap VersionMap,
    ImmutableConditionStack ConditionStack,
    UnmergedCondition? UnmergedCondition)
{
    public IEnumerable<Term> GetConditions()
    {
        if (UnmergedCondition is not null)
        {
            throw new InvalidOperationException("Unmerged condition is not yet merged.");
        }

        return ConditionStack.GetConditions();
    }
}
