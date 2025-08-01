using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Abstractions.Models;
using Slicito.Common.Models;
using Slicito.ProgramAnalysis;
using Slicito.ProgramAnalysis.Reachability;

namespace Slicito.Common.Extensibility;

public abstract class ProgramAnalysisContextBase : IProgramAnalysisContext
{
    private readonly ConcurrentDictionary<Type, object> _services = new();

    public ProgramAnalysisContextBase(ITypeSystem typeSystem, ISliceManager sliceManager, IProgramTypes programTypes)
    {
        SetService(typeSystem);
        SetService(sliceManager);
        SetService(programTypes);

        SetService<IModelCreator<ReachabilityResult>>(new ReachabilityResultModelCreator());
    }

    public ITypeSystem TypeSystem => GetService<ITypeSystem>();

    public ISliceManager SliceManager => GetService<ISliceManager>();

    public abstract ISlice WholeSlice { get; }

    public IProgramTypes ProgramTypes => GetService<IProgramTypes>();

    public abstract IFlowGraphProvider FlowGraphProvider { get; }

    public TInterface GetService<TInterface>() => (TInterface) GetService(typeof(TInterface));

    public object GetService(Type type)
    {
        if (!_services.TryGetValue(type, out var service))
        {
            throw new InvalidOperationException($"Service of type {type.FullName} not registered.");
        }

        return service;
    }

    public bool TryGetService<T>([NotNullWhen(true)] out T? service)
    {
        if (!_services.TryGetValue(typeof(T), out var serviceObject))
        {
            service = default;
            return false;
        }

        service = (T) serviceObject;
        return true;
    }

    public bool TryGetService(Type type, [NotNullWhen(true)] out object? service) => _services.TryGetValue(type, out service);

    public void SetService<TInterface>(TInterface service) where TInterface : notnull
    {
        if (!_services.TryAdd(typeof(TInterface), service))
        {
            throw new InvalidOperationException($"Service of type {typeof(TInterface).FullName} already registered.");
        }
    }
}
