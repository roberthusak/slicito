namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;

public sealed class SmtLibCliSolverFactory(string pathToSolver, string[]? arguments = null) : ISolverFactory
{
    public async ValueTask<ISolver> CreateSolverAsync() => await SmtLibCliSolver.CreateAsync(pathToSolver, arguments);
}
