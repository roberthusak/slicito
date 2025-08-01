using System.Collections.Immutable;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Common.Implementation;

public abstract partial class SliceFragmentBase(
    ISlice slice,
    Dictionary<Type, ElementType> elementTypeMap,
    Dictionary<ElementType, ImmutableArray<string>> elementTypeToAttributeNames) : ITypedSliceFragment
{
    public ISlice Slice { get; } = slice;

    /// <remarks>
    /// Designed to be called by the generated implementation of an interface method such as:
    /// <code>
    /// ValueTask&lt;IEnumerable&lt;IProjectElement&gt;&gt; GetProjectsAsync();
    /// </code>
    /// The generated implementation is equivalent to:
    /// <code>
    /// public ValueTask&lt;IEnumerable&lt;IProjectElement&gt;&gt; GetProjectsAsync()
    /// {
    ///     return GetRootElementsAsync&lt;IProjectElement&gt;((id, attributes) => new ProjectElement(id, attributes[0]));
    /// }
    /// </code>
    /// </remarks>
    protected async ValueTask<IEnumerable<TElement>> GetRootElementsAsync<TElement>(Func<ElementId, string[], TElement> elementFactory)
        where TElement : IElement
    {
        var elementType = elementTypeMap[typeof(TElement)];

        var elements = (await Slice.GetRootElementsAsync(elementType)).ToArray();

        var attributeNames = elementTypeToAttributeNames[elementType];

        if (attributeNames.Length == 0)
        {
            return elements.Select(element => elementFactory(element.Id, []));
        }
        else
        {
            var attributeValues = new string[elements.Length][];
            for (var i = 0; i < elements.Length; i++)
            {
                attributeValues[i] = new string[attributeNames.Length];
            }

            for (var attributeIndex = 0; attributeIndex < attributeNames.Length; attributeIndex++)
            {
                var attributeProvider = Slice.GetElementAttributeProviderAsyncCallback(attributeNames[attributeIndex]);

                for (var elementIndex = 0; elementIndex < elements.Length; elementIndex++)
                {
                    attributeValues[elementIndex][attributeIndex] = await attributeProvider(elements[elementIndex].Id);
                }
            }

            return elements.Select((element, i) => elementFactory(element.Id, attributeValues[i]));
        }
    }
}
