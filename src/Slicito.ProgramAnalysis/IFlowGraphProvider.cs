using Slicito.Abstractions;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis;

public interface IFlowGraphProvider
{
    IFlowGraph? TryGetFlowGraph(ElementId elementId);

    ProcedureSignature GetProcedureSignature(ElementId elementId);
}
