using System.Collections.Immutable;

namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

public abstract record Term
{
    private Term() { }

    public abstract Sort Sort { get; }

    public abstract record Constant : Term
    {
        private Constant() { }

        public sealed record Bool(bool Value) : Constant
        {
            public override Sort Sort => Sorts.Bool;
        }

        public sealed record Int(long Value) : Constant
        {
            public override Sort Sort => Sorts.Int;
        }


        public sealed record BitVec(ulong Value, Sort.BitVec BitVecSort) : Constant
        {
            public override Sort Sort => BitVecSort;
        }
    }

    public sealed record FunctionApplication(Function function, ImmutableArray<Term> Arguments) : Term
    {
        public override Sort Sort => function.ResultSort;
    }
}
