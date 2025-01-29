namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

public abstract record Sort
{
    private Sort() { }

    public sealed record Bool : Sort;

    public sealed record Int : Sort;

    public sealed record BitVec(int Width) : Sort;

    public sealed record String : Sort;

    public sealed record RegLan : Sort;
}
