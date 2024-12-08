using Slicito.Abstractions;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis;

public interface ICallGraphProvider
{
    IFlowGraph? TryGetFlowGraph(ElementId elementId);

    ProcedureSignature GetProcedureSignature(ElementId elementId);
}
