using Slicito.Abstractions.Queries;

namespace Slicito.Abstractions;

public interface ISlicitoContext
{
    ITypeSystem TypeSystem { get; }

    ISliceManager SliceManager { get; }

    ILazySlice WholeSlice { get; }

    T GetService<T>();
}
