using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis.DataFlow;

public interface IDataFlowAnalysis<TDomain>
{
    AnalysisDirection Direction { get; }

    void Initialize(IFlowGraph graph);

    TDomain GetInitialInputValue(BasicBlock block);

    TDomain GetInitialOutputValue(BasicBlock block);

    TDomain Transfer(BasicBlock block, TDomain value);

    TDomain Meet(TDomain left, TDomain right);

    bool Equals(TDomain left, TDomain right);
}
