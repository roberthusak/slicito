namespace Slicito.Abstractions;

public interface ISliceManager
{
    ISliceBuilder CreateBuilder();

    TSliceFragmentBuilder CreateTypedBuilder<TSliceFragmentBuilder>();
}
