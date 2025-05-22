using System.Diagnostics;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Common.Implementation;

public abstract partial class SliceFragmentBuilderBase(
    Dictionary<Type, ElementType> elementTypeMap,
    Func<ISlice, SliceFragmentBase> sliceFragmentFactory)
{
    private readonly ISliceBuilder _sliceBuilder = new SliceBuilder();

    private readonly Dictionary<ElementType, List<ISliceBuilder.PartialElementInfo>> _elementTypeToRootElements = [];

    private readonly Dictionary<(ElementType, string), Dictionary<ElementId, string>> _elementAttributeToElementIdToAttributeValue = [];

    /// <remarks>
    /// Designed to be called by the generated implementation of an interface method such as:
    /// <code>
    /// [RootElement(typeof(IProjectElement))]
    /// IProjectSliceFragmentBuilder AddProject(ElementId id, string name);
    /// </code>
    /// The generated implementation is equivalent to:
    /// <code>
    /// public IProjectSliceFragmentBuilder AddProject(ElementId id, string name)
    /// {
    ///     AddRootElement&lt;IProjectElement&gt;(id, ["Name", name]);
    ///     return this;
    /// }
    /// </code>
    /// </remarks>
    protected void AddRootElement<TElement>(ElementId id, string[] attributeKeysAndValues)
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

        Debug.Assert(attributeKeysAndValues.Length % 2 == 0);

        for (var i = 0; i < attributeKeysAndValues.Length; i += 2)
        {
            var attributeName = attributeKeysAndValues[i];
            var attributeValue = attributeKeysAndValues[i + 1];

            if (!_elementAttributeToElementIdToAttributeValue.TryGetValue((elementType, attributeName), out var elementIdToAttributeValue))
            {
                elementIdToAttributeValue = [];
                _elementAttributeToElementIdToAttributeValue[(elementType, attributeName)] = elementIdToAttributeValue;

                _sliceBuilder.AddElementAttribute(elementType, attributeName, id => elementIdToAttributeValue[id]);
            }

            elementIdToAttributeValue[id] = attributeValue;
        }
    }

    /// <remarks>
    /// Designed to be called by the generated implementation of an interface method such as:
    /// <code>
    /// ValueTask&lt;ITestProjectSliceFragment&gt; BuildAsync();
    /// </code>
    /// The generated implementation is equivalent to:
    /// <code>
    /// public ValueTask&lt;ITestProjectSliceFragment&gt; BuildAsync()
    /// {
    ///     return BuildAsync&lt;ITestProjectSliceFragment&gt;();
    /// }
    /// </code>
    /// </remarks>
    protected ValueTask<TSliceFragment> BuildAsync<TSliceFragment>()
        where TSliceFragment : ITypedSliceFragment
    {
        var slice = _sliceBuilder.Build();
        return new((TSliceFragment)(object)sliceFragmentFactory(slice));
    }
}
