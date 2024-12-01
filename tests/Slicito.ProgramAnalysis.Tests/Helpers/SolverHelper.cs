using Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;

namespace Slicito.ProgramAnalysis.Tests.Helpers;

internal static class SolverHelper
{
    // The test expects the Z3 solver to be available in the system path.
    public static SmtLibCliSolverFactory CreateSolverFactory(TestContext testContext) =>
        new("z3", ["-in"], line => testContext.WriteLine($"Z3: {line}"));
}
