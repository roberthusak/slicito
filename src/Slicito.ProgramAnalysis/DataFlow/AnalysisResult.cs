using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis.DataFlow;

public class AnalysisResult<TDomain>(
    IReadOnlyDictionary<BasicBlock, TDomain> inputMap,
    IReadOnlyDictionary<BasicBlock, TDomain> outputMap)
{
    public IReadOnlyDictionary<BasicBlock, TDomain> InputMap { get; } = inputMap;
    public IReadOnlyDictionary<BasicBlock, TDomain> OutputMap { get; } = outputMap;
}
