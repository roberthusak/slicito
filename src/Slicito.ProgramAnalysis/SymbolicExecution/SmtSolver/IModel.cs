using Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;

public interface IModel : IDisposable
{
    Term Evaluate(Term term);
}
