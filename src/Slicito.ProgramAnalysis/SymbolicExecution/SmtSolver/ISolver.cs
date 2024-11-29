using Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;

public interface ISolver : IDisposable
{
    ValueTask AssertAsync(Term term);

    ValueTask<SolverResult> CheckSatAsync(Func<IModel, ValueTask>? onSat = null);
}
