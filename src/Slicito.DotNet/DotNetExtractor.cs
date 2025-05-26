using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.Abstractions.Collections;

namespace Slicito.DotNet;

public class DotNetExtractor(DotNetTypes types, ISliceManager sliceManager)
{
    private readonly DotNetTypes _types = types;
    private readonly ISliceManager _sliceManager = sliceManager;

    private readonly ConcurrentDictionary<ContentEquatableArray<Solution>, DotNetSolutionContext> _solutionsSliceCache = [];

    public DotNetSolutionContext Extract(ImmutableArray<Solution> solutions) =>
        _solutionsSliceCache.GetOrAdd(solutions, _ => new DotNetSolutionContext(solutions, _types, _sliceManager));
}
