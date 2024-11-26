using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Implementation;

namespace Slicito.DotNet;

public class DotNetSolutionContext(Solution solution, DotNetTypes types, ISliceManager sliceManager)
{
    private readonly SliceCreator _sliceCreator = new(solution, types, sliceManager);

    public ILazySlice LazySlice => _sliceCreator.LazySlice;
}
