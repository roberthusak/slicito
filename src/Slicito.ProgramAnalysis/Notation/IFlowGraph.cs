namespace Slicito.ProgramAnalysis.Notation;

public interface IFlowGraph
{
    BasicBlock.Entry Entry { get; }
    BasicBlock.Exit Exit { get; }
    IEnumerable<BasicBlock> Blocks { get; }

    BasicBlock? GetTrueSuccessor(BasicBlock block);
    BasicBlock? GetFalseSuccessor(BasicBlock block);
    BasicBlock? GetUnconditionalSuccessor(BasicBlock block);

    IEnumerable<BasicBlock> GetSuccessors(BasicBlock block);
    IEnumerable<BasicBlock> GetPredecessors(BasicBlock block);

    public interface IBuilder : IFlowGraph
    {
        void AddBlock(BasicBlock.Inner block);

        void AddTrueEdge(BasicBlock source, BasicBlock target);
        void AddFalseEdge(BasicBlock source, BasicBlock target);
        void AddUnconditionalEdge(BasicBlock source, BasicBlock target);
    }
}
