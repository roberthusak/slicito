using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Implementation;
using Slicito.ProgramAnalysis;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.DotNet;

public class DotNetSolutionContext(Solution solution, DotNetTypes types, ISliceManager sliceManager) : IFlowGraphProvider
{
    private readonly SliceCreator _sliceCreator = new(solution, types, sliceManager);

    public Solution Solution => solution;

    public ILazySlice LazySlice => _sliceCreator.LazySlice;

    public IFlowGraph? TryGetFlowGraph(ElementId elementId) => _sliceCreator.TryCreateFlowGraph(elementId);

    public ProcedureSignature GetProcedureSignature(ElementId elementId) => _sliceCreator.GetProcedureSignature(elementId);
}
