using Slicito.Abstractions;
using Slicito.Abstractions.Facts.Attributes;
using Slicito.Abstractions.Interaction;
using Slicito.DotNet.Facts;

namespace Slicito.DotNet;

public interface IDotNetSliceFragment : ITypedSliceFragment
{
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
    ValueTask<IEnumerable<ICSharpFieldElement>> GetFieldsAsync(ICSharpTypeElement type);

    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ICSharpMethodElement>> GetMethodsAsync(ICSharpTypeElement type);

    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ICSharpLocalFunctionElement>> GetLocalFunctionsAsync(ICSharpMethodElement method);

    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ICSharpLambdaElement>> GetLambdasAsync(ICSharpMethodElement method);
    
    [ForwardLinkKind(CommonAttributeValues.Kind.Contains)]
    ValueTask<IEnumerable<ICSharpOperationElement>> GetOperationsAsync(ICSharpProcedureElement function);

    [ForwardLinkKind(DotNetAttributeValues.Kind.Overrides)]
    ValueTask<IEnumerable<IDotNetMethodElement>> GetOverridenMethodsAsync(IDotNetMethodElement method);

    [return: Attribute(CommonAttributeNames.CodeLocation)]
    ValueTask<CodeLocation> GetCodeLocationAsync(ICSharpTypeElement type);

    [return: Attribute(CommonAttributeNames.CodeLocation)]
    ValueTask<CodeLocation> GetCodeLocationAsync(ICSharpPropertyElement property);

    [return: Attribute(CommonAttributeNames.CodeLocation)]
    ValueTask<CodeLocation> GetCodeLocationAsync(ICSharpFieldElement field);

    [return: Attribute(CommonAttributeNames.CodeLocation)]
    ValueTask<CodeLocation> GetCodeLocationAsync(ICSharpMethodElement method);
}
