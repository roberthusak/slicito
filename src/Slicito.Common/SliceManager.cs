using Slicito.Abstractions;

namespace Slicito.Common;

public class SliceManager : ISliceManager
{
    public ISliceBuilder CreateBuilder() => new SliceBuilder();
}
