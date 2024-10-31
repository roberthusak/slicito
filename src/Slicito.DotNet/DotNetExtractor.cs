using System.Collections.Concurrent;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Implementation;

namespace Slicito.DotNet;

public class DotNetExtractor
{
    private readonly DotNetTypes _types;
    private readonly ISliceManager _sliceManager;

    private readonly ConcurrentDictionary<Solution, ILazySlice> _solutionSliceCache = [];

    public DotNetExtractor(DotNetTypes types, ISliceManager sliceManager)
    {
        _types = types;
        _sliceManager = sliceManager;
    }

    public ILazySlice Extract(Solution solution) =>
        _solutionSliceCache.GetOrAdd(solution, _ => SliceCreator.CreateSlice(solution, _types, _sliceManager));
}
