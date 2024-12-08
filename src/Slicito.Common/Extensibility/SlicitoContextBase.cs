using System.Collections.Concurrent;

using Slicito.Abstractions;
using Slicito.Abstractions.Queries;
using Slicito.ProgramAnalysis;

namespace Slicito.Common.Extensibility;

public abstract class ProgramAnalysisContextBase : IProgramAnalysisContext
{
    private readonly ConcurrentDictionary<Type, object> _services = new();

    public ProgramAnalysisContextBase(ITypeSystem typeSystem, ISliceManager sliceManager, IProgramTypes programTypes)
    {
        SetService(typeSystem);
        SetService(sliceManager);
        SetService(programTypes);
    }

    public ITypeSystem TypeSystem => GetService<ITypeSystem>();

    public ISliceManager SliceManager => GetService<ISliceManager>();

    public abstract ILazySlice WholeSlice { get; }

    public IProgramTypes ProgramTypes => GetService<IProgramTypes>();

    public abstract ICallGraphProvider CallGraphProvider { get; }

    public TInterface GetService<TInterface>()
    {
        if (!_services.TryGetValue(typeof(TInterface), out var service))
        {
            throw new InvalidOperationException($"Service of type {typeof(TInterface).FullName} not registered.");
        }

        return (TInterface)service;
    }

    public void SetService<TInterface>(TInterface service) where TInterface : notnull
    {
        if (!_services.TryAdd(typeof(TInterface), service))
        {
            throw new InvalidOperationException($"Service of type {typeof(TInterface).FullName} already registered.");
        }
    }
}
