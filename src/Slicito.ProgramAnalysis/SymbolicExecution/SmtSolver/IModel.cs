using Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;

public interface IModel : IDisposable
{
    ValueTask<Term> EvaluateAsync(Term term);
}
