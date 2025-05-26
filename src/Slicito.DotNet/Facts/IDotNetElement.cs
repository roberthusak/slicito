using Slicito.Abstractions.Facts;
using Slicito.Abstractions.Facts.Attributes;
using Slicito.DotNet.Facts.Attributes;

namespace Slicito.DotNet.Facts;

[Runtime(DotNetAttributeValues.DotNetRuntime)]
public interface IDotNetElement : IRuntimeElement
{
}

[Language(DotNetAttributeValues.CSharpLanguage)]
public interface ICSharpElement : IDotNetElement, ILanguageElement
{
}

[Kind(DotNetAttributeValues.SolutionKind)]
public interface ISolutionElement : INamedElement
{
}

[Kind(DotNetAttributeValues.ProjectKind)]
public interface ICSharpProjectElement : ICSharpElement, INamedElement
{
    ProjectOutputKind OutputKind { get; }
}

[Kind(DotNetAttributeValues.NamespaceKind)]
public interface IDotNetNamespaceElement : IDotNetElement, INamedElement
{
}

public interface ICSharpNamespaceElement : IDotNetNamespaceElement, ICSharpElement
{
}

[Kind(DotNetAttributeValues.TypeKind)]
public interface IDotNetTypeElement : IDotNetElement, INamedElement
{
}

public interface ICSharpTypeElement : IDotNetTypeElement, ICSharpElement
{
}

[Kind(DotNetAttributeValues.PropertyKind)]
public interface IDotNetPropertyElement : IDotNetElement, INamedElement
{
}

public interface ICSharpPropertyElement : IDotNetPropertyElement, ICSharpElement
{
}

[Kind(DotNetAttributeValues.FieldKind)]
public interface IDotNetFieldElement : IDotNetElement, INamedElement
{
}

public interface ICSharpFieldElement : IDotNetFieldElement, ICSharpElement
{
}

[Kind(DotNetAttributeValues.MethodKind)]
public interface IDotNetMethodElement : IDotNetElement, INamedElement
{
}

public interface ICSharpMethodElement : IDotNetMethodElement, ICSharpElement
{
}

[Kind(DotNetAttributeValues.OperationKind)]
public interface ICSharpOperationElement : ICSharpElement
{
}

[OperationKind(DotNetAttributeValues.AssignmentOperationKind)]
public interface ICSharpAssignmentElement : ICSharpOperationElement
{
}

[OperationKind(DotNetAttributeValues.ConditionalJumpOperationKind)]
public interface ICSharpConditionalJumpElement : ICSharpOperationElement
{
}

[OperationKind(DotNetAttributeValues.CallOperationKind)]
public interface ICSharpCallElement : ICSharpOperationElement
{
}
