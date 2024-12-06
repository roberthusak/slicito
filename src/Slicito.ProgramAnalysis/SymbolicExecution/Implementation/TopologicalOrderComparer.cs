using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis.SymbolicExecution.Implementation;

internal sealed class TopologicalOrderComparer(Dictionary<BasicBlock, int> topologicalOrder) : IComparer<ExecutionState>
{
    public int Compare(ExecutionState x, ExecutionState y)
    {
        var orderComparison = topologicalOrder[x.CurrentBlock].CompareTo(topologicalOrder[y.CurrentBlock]);
        if (orderComparison != 0)
        {
            return orderComparison;
        }

        // Makes sure that a set can contain more states with the same block
        return x.GetHashCode().CompareTo(y.GetHashCode());
    }
}
