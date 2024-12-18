using System.Collections.Immutable;

namespace Slicito.ProgramAnalysis.Notation;

public abstract class BasicBlock
{
    private BasicBlock() { }

    public sealed class Entry(ImmutableArray<Variable> parameters) : BasicBlock
    {
        public ImmutableArray<Variable> Parameters { get; } = parameters;
    }

    public sealed class Exit(ImmutableArray<Expression> returnValues) : BasicBlock
    {
        public ImmutableArray<Expression> ReturnValues { get; } = returnValues;
    }

    public sealed class Inner(Operation? operation) : BasicBlock
    {
        public Operation? Operation { get; } = operation;
    }
}
