using Microsoft.CodeAnalysis.MSBuild;
using Slicito.Abstractions;
using Slicito.Abstractions.Queries;
using Slicito.DotNet;
using Slicito.ProgramAnalysis.Notation;
using Slicito.Queries;

using System.IO;
using System.Reflection;

namespace StandaloneGui;

public class DependencyManager
{
    private readonly string _solutionPath;
    private readonly ITypeSystem _typeSystem;
    private readonly ISliceManager _sliceManager;
    
    private DotNetTypes? _dotNetTypes;
    private DotNetExtractor? _dotNetExtractor;
    private DotNetSolutionContext? _dotNetSolutionContext;
    private ILazySlice? _lazySlice;
    private DotNetFactProvider? _factProvider;

    public DependencyManager(string solutionPath)
    {
        _solutionPath = solutionPath;
        _typeSystem = new TypeSystem();
        _sliceManager = new SliceManager();
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
            var t when t == typeof(ILazySlice) => await TryLoadLazySliceAsync(),
            var t when t == typeof(IFlowGraph) => null,
            var t when t == typeof(DotNetFactProvider) => await LoadFactProviderAsync(),
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
            _dotNetSolutionContext = GetDotNetExtractor().Extract(solution);
        }

        return _dotNetSolutionContext;
    }

    private async Task<ILazySlice?> TryLoadLazySliceAsync()
    {
        _lazySlice ??= (await TryGetDotNetSolutionContextAsync())?.LazySlice;
        return _lazySlice;
    }

    private async Task<DotNetFactProvider> LoadFactProviderAsync()
    {
        if (_factProvider == null)
        {
            var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(_solutionPath);
            _factProvider = new DotNetFactProvider(solution);
        }

        return _factProvider;
    }
} 
