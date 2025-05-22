using Slicito.Abstractions;
using Slicito.Abstractions.Facts;

namespace Slicito.Common.Implementation;

public abstract partial class SliceFragmentBase(ISlice slice, Dictionary<Type, ElementType> elementTypeMap) : ITypedSliceFragment
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
    ///     return GetRootElementsAsync&lt;IProjectElement&gt;(id => new ProjectElement(id));
    /// }
    /// </code>
    /// </remarks>
    protected async ValueTask<IEnumerable<TElement>> GetRootElementsAsync<TElement>(Func<ElementId, TElement> elementFactory)
        where TElement : IElement
    {
        var elementType = elementTypeMap[typeof(TElement)];

        var elements = await Slice.GetRootElementsAsync(elementType);

        return elements.Select(element => elementFactory(element.Id));
    }
}
