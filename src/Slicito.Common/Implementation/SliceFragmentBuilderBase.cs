using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Common.Implementation;

public abstract partial class SliceFragmentBuilderBase(
    Dictionary<Type, ElementType> elementTypeMap,
    Func<ISlice, SliceFragmentBase> sliceFragmentFactory)
{
    private readonly ISliceBuilder _sliceBuilder = new SliceBuilder();

    private readonly Dictionary<ElementType, List<ISliceBuilder.PartialElementInfo>> _elementTypeToRootElements = [];

    protected void AddRootElement<TElement>(ElementId id)
        where TElement : IElement
    {
        var elementType = elementTypeMap[typeof(TElement)];
        if (!_elementTypeToRootElements.TryGetValue(elementType, out var elementIds))
        {
            elementIds = [];
            _elementTypeToRootElements[elementType] = elementIds;

            _sliceBuilder.AddRootElements(
                elementType,
                () => elementIds);
        }

        elementIds.Add(new(id));
    }

    protected ValueTask<TSliceFragment> BuildAsync<TSliceFragment>()
        where TSliceFragment : ITypedSliceFragment
    {
        var slice = _sliceBuilder.Build();
        return new((TSliceFragment)(object)sliceFragmentFactory(slice));
    }
}
