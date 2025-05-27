using Slicito.Abstractions;
using Slicito.Abstractions.Facts.Attributes;
using Slicito.DotNet.Facts;

namespace Slicito.DotNet;

public interface IDotNetSliceFragment : ITypedSliceFragment
{
    [ForwardLinkKind(CommonAttributeNames.Contains)]
    ValueTask<IEnumerable<ISolutionElement>> GetSolutionsAsync();

    [ForwardLinkKind(CommonAttributeNames.Contains)]
    ValueTask<IEnumerable<ICSharpProjectElement>> GetProjectsAsync(ISolutionElement solution);

    [ForwardLinkKind(DotNetAttributeNames.References)]
    ValueTask<IEnumerable<ICSharpProjectElement>> GetReferencedProjectsAsync(ICSharpProjectElement project);

    [ForwardLinkKind(CommonAttributeNames.Contains)]
    ValueTask<IEnumerable<ICSharpNamespaceElement>> GetNamespacesAsync(ICSharpProjectElement project);

    [ForwardLinkKind(CommonAttributeNames.Contains)]
    ValueTask<IEnumerable<ICSharpNamespaceElement>> GetNamespacesAsync(ICSharpNamespaceElement @namespace);

    [ForwardLinkKind(CommonAttributeNames.Contains)]
    ValueTask<IEnumerable<ICSharpTypeElement>> GetTypesAsync(ICSharpNamespaceElement @namespace);

    [ForwardLinkKind(CommonAttributeNames.Contains)]
    ValueTask<IEnumerable<ICSharpTypeElement>> GetTypesAsync(ICSharpTypeElement type);

    [ForwardLinkKind(CommonAttributeNames.Contains)]
    ValueTask<IEnumerable<ICSharpPropertyElement>> GetPropertiesAsync(ICSharpTypeElement type);

    [ForwardLinkKind(CommonAttributeNames.Contains)]
    ValueTask<IEnumerable<ICSharpPropertyElement>> GetPropertiesAsync(ICSharpPropertyElement property);

    [ForwardLinkKind(CommonAttributeNames.Contains)]
    ValueTask<IEnumerable<ICSharpMethodElement>> GetMethodsAsync(ICSharpTypeElement type);

    [ForwardLinkKind(CommonAttributeNames.Contains)]
    ValueTask<IEnumerable<ICSharpOperationElement>> GetOperationsAsync(ICSharpMethodElement method);
}
