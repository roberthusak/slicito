using System.Collections.Immutable;

namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

public abstract record Term
{
    private Term() { }

    public abstract record Constant : Term
    {
        private Constant() { }

        public sealed record Bool(bool Value) : Constant;

        public sealed record BitVec(long Value, Sort.BitVec Sort) : Constant;
    }

    public sealed record FunctionApplication(Function function, ImmutableArray<Term> Arguments) : Term;
}
