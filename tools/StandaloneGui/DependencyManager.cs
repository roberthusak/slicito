using Microsoft.CodeAnalysis.MSBuild;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Abstractions.Interaction;
using Slicito.Common;
using Slicito.DotNet;
using Slicito.ProgramAnalysis.Notation;

using System.IO;
using System.Reflection;

namespace StandaloneGui;

public class DependencyManager
{
    private readonly string _solutionPath;
    private readonly ITypeSystem _typeSystem;
    private readonly ISliceManager _sliceManager;
    private readonly ICodeNavigator _codeNavigator;

    private DotNetTypes? _dotNetTypes;
    private DotNetExtractor? _dotNetExtractor;
    private DotNetSolutionContext? _dotNetSolutionContext;
    private ISlice? _slice;

    private readonly ICache _cache;

    public DependencyManager(string solutionPath)
    {
        _solutionPath = solutionPath;
        _typeSystem = new TypeSystem();
        _sliceManager = new SliceManager(_typeSystem);
        _codeNavigator = new ExternalVisualStudioCodeNavigator();
        _cache = new Cache();
    }

    public async Task<object?[]> ResolveDependenciesAsync(ConstructorInfo constructor)
    {
        var dependencies = new List<object?>();

        foreach (var parameter in constructor.GetParameters())
        {
            dependencies.Add(await ResolveDependencyAsync(parameter.ParameterType));
        }

        return [.. dependencies];
    }

    private async Task<object?> ResolveDependencyAsync(Type parameterType)
    {
        return parameterType switch
        {
            var t when t == typeof(ITypeSystem) => _typeSystem,
            var t when t == typeof(DotNetTypes) => GetDotNetTypes(),
            var t when t == typeof(DotNetExtractor) => GetDotNetExtractor(),
            var t when t == typeof(DotNetSolutionContext) => await TryGetDotNetSolutionContextAsync(),
            var t when t == typeof(ISlice) => await TryLoadSliceAsync(),
            var t when t == typeof(ISliceManager) => _sliceManager,
            var t when t == typeof(IFlowGraph) => null,
            var t when t == typeof(ICodeNavigator) => _codeNavigator,
            var t when t == typeof(ICache) => _cache,
            _ => throw new ApplicationException($"Unsupported parameter type {parameterType.Name}.")
        };
    }

    private DotNetTypes GetDotNetTypes()
    {
        _dotNetTypes ??= new DotNetTypes(_typeSystem);
        return _dotNetTypes;
    }

    private DotNetExtractor GetDotNetExtractor()
    {
        _dotNetExtractor ??= new DotNetExtractor(GetDotNetTypes(), _sliceManager);
        return _dotNetExtractor;
    }

    private async Task<DotNetSolutionContext?> TryGetDotNetSolutionContextAsync()
    {
        if (_dotNetSolutionContext == null && File.Exists(_solutionPath))
        {
            var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(_solutionPath);
            _dotNetSolutionContext = GetDotNetExtractor().Extract([solution]);
        }

        return _dotNetSolutionContext;
    }

    private async Task<ISlice?> TryLoadSliceAsync()
    {
        _slice ??= (await TryGetDotNetSolutionContextAsync())?.Slice;
        return _slice;
    }
} 
