using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Implementation;
using Slicito.ProgramAnalysis;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.DotNet;

public class DotNetSolutionContext(ImmutableArray<Solution> solutions, DotNetTypes types, ISliceManager sliceManager) : IFlowGraphProvider
{
    private readonly SliceCreator _sliceCreator = new(solutions, types, sliceManager);

    public ImmutableArray<Solution> Solutions => solutions;

    public ISlice Slice => _sliceCreator.Slice;

    public IFlowGraph? TryGetFlowGraph(ElementId elementId) => _sliceCreator.TryCreateFlowGraph(elementId);

    public ProcedureSignature GetProcedureSignature(ElementId elementId) => _sliceCreator.GetProcedureSignature(elementId);

    public Project GetProject(ElementId elementId) => _sliceCreator.GetProject(elementId);

    public ISymbol GetSymbol(ElementId elementId) => _sliceCreator.GetSymbol(elementId);
}
