namespace Slicito.ProgramAnalysis.Notation;

public abstract class BasicBlock
{
    private BasicBlock() { }

    public sealed class Entry : BasicBlock;

    public sealed class Exit : BasicBlock;

    public sealed class Inner(Operation? operation) : BasicBlock
    {
        public Operation? Operation { get; } = operation;
    }
}
