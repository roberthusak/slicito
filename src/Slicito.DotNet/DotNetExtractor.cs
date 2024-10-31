using System.Collections.Concurrent;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Implementation;

namespace Slicito.DotNet;

public class DotNetExtractor(DotNetTypes types, ISliceManager sliceManager)
{
    private readonly DotNetTypes _types = types;
    private readonly ISliceManager _sliceManager = sliceManager;

    private readonly ConcurrentDictionary<Solution, ILazySlice> _solutionSliceCache = [];

    public ILazySlice Extract(Solution solution) =>
        _solutionSliceCache.GetOrAdd(solution, _ => SliceCreator.CreateSlice(solution, _types, _sliceManager));
}
