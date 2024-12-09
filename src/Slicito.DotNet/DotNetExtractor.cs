using System.Collections.Concurrent;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.DotNet;

public class DotNetExtractor(DotNetTypes types, ISliceManager sliceManager)
{
    private readonly DotNetTypes _types = types;
    private readonly ISliceManager _sliceManager = sliceManager;

    private readonly ConcurrentDictionary<Solution, DotNetSolutionContext> _solutionSliceCache = [];

    public DotNetSolutionContext Extract(Solution solution) =>
        _solutionSliceCache.GetOrAdd(solution, _ => new DotNetSolutionContext(_, _types, _sliceManager));
}
