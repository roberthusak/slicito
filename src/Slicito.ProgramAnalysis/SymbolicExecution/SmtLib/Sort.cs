namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

public abstract record Sort
{
    private Sort() { }

    public sealed record Bool : Sort;

    public sealed record BitVec(int Width) : Sort;
}
