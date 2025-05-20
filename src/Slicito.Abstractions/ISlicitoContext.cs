using System.Diagnostics.CodeAnalysis;

using Slicito.Abstractions.Queries;

namespace Slicito.Abstractions;

public interface ISlicitoContext
{
    ITypeSystem TypeSystem { get; }

    ISliceManager SliceManager { get; }

    ISlice WholeSlice { get; }

    T GetService<T>();

    object GetService(Type type);

    bool TryGetService<T>([NotNullWhen(true)] out T? service);

    bool TryGetService(Type type, [NotNullWhen(true)] out object? service);
}
