using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Implementation;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.DotNet;

public class DotNetSolutionContext(Solution solution, DotNetTypes types, ISliceManager sliceManager)
{
    private readonly SliceCreator _sliceCreator = new(solution, types, sliceManager);

    public ILazySlice LazySlice => _sliceCreator.LazySlice;

    public IFlowGraph? TryGetFlowGraph(ElementId elementId) => _sliceCreator.TryCreateFlowGraph(elementId);
}
