using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Common.Implementation;

public abstract partial class SliceFragmentBase(ISlice slice, Dictionary<Type, ElementType> elementTypeMap) : ITypedSliceFragment
{
    public ISlice Slice { get; } = slice;

    protected async ValueTask<IEnumerable<TElement>> GetRootElementsAsync<TElement>(Func<ElementId, TElement> elementFactory)
        where TElement : IElement
    {
        var elementType = elementTypeMap[typeof(TElement)];

        var elements = await Slice.GetRootElementsAsync(elementType);

        return elements.Select(element => elementFactory(element.Id));
    }
}
