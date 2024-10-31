using Slicito.Abstractions;

namespace Slicito.Queries;

public class SliceManager : ISliceManager
{
    public ISliceBuilder CreateBuilder() => new SliceBuilder();
}
