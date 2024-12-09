namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;

public interface ISolverFactory
{
    ValueTask<ISolver> CreateSolverAsync();
}
