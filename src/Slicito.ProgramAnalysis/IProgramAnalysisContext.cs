using Slicito.Abstractions;

namespace Slicito.ProgramAnalysis;

public interface IProgramAnalysisContext : ISlicitoContext
{
    IProgramTypes ProgramTypes { get; }

    IFlowGraphProvider FlowGraphProvider { get; }
}
