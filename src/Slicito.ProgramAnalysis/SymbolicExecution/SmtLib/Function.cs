namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

public abstract record Function
{
    private Function() { }

    public abstract string Name { get; init; }

    public abstract bool IsBuiltIn { get; init; }

    public sealed record Nullary(string Name, Sort Sort, bool IsBuiltIn = false) : Function;

    public sealed record Unary(string Name, Sort ArgumentSort, Sort ResultSort, bool IsBuiltIn = false) : Function;

    public sealed record Binary(string Name, Sort ArgumentSort1, Sort ArgumentSort2, Sort ResultSort, bool IsBuiltIn = false) : Function;

    public sealed record Ternary(string Name, Sort ArgumentSort1, Sort ArgumentSort2, Sort ArgumentSort3, Sort ResultSort, bool IsBuiltIn = false) : Function;
}
