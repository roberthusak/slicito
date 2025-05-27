using Slicito.Abstractions;
using Slicito.Abstractions.Facts.Attributes;
using Slicito.DotNet.Facts;

namespace Slicito.DotNet;

public interface IDotNetSliceFragment : ITypedSliceFragment
{
    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ISolutionElement>> GetSolutionsAsync();

    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ICSharpProjectElement>> GetProjectsAsync(ISolutionElement solution);

    [ForwardLinkKind(DotNetAttributeValues.Kind.References)]
    ValueTask<IEnumerable<ICSharpProjectElement>> GetReferencedProjectsAsync(ICSharpProjectElement project);

    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ICSharpNamespaceElement>> GetNamespacesAsync(ICSharpProjectElement project);

    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ICSharpNamespaceElement>> GetNamespacesAsync(ICSharpNamespaceElement @namespace);

    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ICSharpTypeElement>> GetTypesAsync(ICSharpNamespaceElement @namespace);

    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ICSharpTypeElement>> GetTypesAsync(ICSharpTypeElement type);

    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ICSharpPropertyElement>> GetPropertiesAsync(ICSharpTypeElement type);

    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ICSharpPropertyElement>> GetPropertiesAsync(ICSharpPropertyElement property);

    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ICSharpMethodElement>> GetMethodsAsync(ICSharpTypeElement type);

    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ICSharpOperationElement>> GetOperationsAsync(ICSharpMethodElement method);
}
