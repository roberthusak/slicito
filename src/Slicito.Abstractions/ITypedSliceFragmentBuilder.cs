namespace Slicito.Abstractions;

public interface ITypedSliceFragmentBuilder<TSliceFragment>
    where TSliceFragment : ITypedSliceFragment
{
    ValueTask<TSliceFragment> BuildAsync();
}
