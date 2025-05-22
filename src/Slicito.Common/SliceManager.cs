using Slicito.Abstractions;

namespace Slicito.Common;

public class SliceManager(ITypeSystem typeSystem) : ISliceManager
{
    public ISliceBuilder CreateBuilder() => new SliceBuilder();
}
