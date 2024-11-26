namespace Slicito.ProgramAnalysis.Notation;

public interface IFlowGraphBuilder : IFlowGraph
{
    void AddBlock(BasicBlock.Inner block);

    void AddTrueEdge(BasicBlock source, BasicBlock target);
    void AddFalseEdge(BasicBlock source, BasicBlock target);
    void AddUnconditionalEdge(BasicBlock source, BasicBlock target);

    IFlowGraph Build();
}
