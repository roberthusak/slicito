using Slicito.Abstractions.Facts;
using Slicito.Abstractions.Facts.Attributes;
using Slicito.DotNet.Facts.Attributes;

namespace Slicito.DotNet.Facts;

[Runtime(DotNetAttributeValues.Runtime.DotNet)]
public interface IDotNetElement : IRuntimeElement
{
}

[Language(DotNetAttributeValues.Language.CSharp)]
public interface ICSharpElement : IDotNetElement, ILanguageElement
{
}

[Kind(DotNetAttributeValues.Kind.Solution)]
public interface ISolutionElement : INamedElement
{
}

[Kind(DotNetAttributeValues.Kind.Project)]
public interface ICSharpProjectElement : ICSharpElement, INamedElement
{
    ProjectOutputKind OutputKind { get; }
}

[Kind(DotNetAttributeValues.Kind.Namespace)]
public interface IDotNetNamespaceElement : IDotNetElement, INamedElement
{
}

public interface ICSharpNamespaceElement : IDotNetNamespaceElement, ICSharpElement
{
}

[Kind(DotNetAttributeValues.Kind.Type)]
public interface IDotNetTypeElement : IDotNetElement, INamedElement
{
}

public interface ICSharpTypeElement : IDotNetTypeElement, ICSharpElement
{
}

[Kind(DotNetAttributeValues.Kind.Property)]
public interface IDotNetPropertyElement : IDotNetElement, INamedElement
{
}

public interface ICSharpPropertyElement : IDotNetPropertyElement, ICSharpElement
{
}

[Kind(DotNetAttributeValues.Kind.Field)]
public interface IDotNetFieldElement : IDotNetElement, INamedElement
{
}

public interface ICSharpFieldElement : IDotNetFieldElement, ICSharpElement
{
}

public interface ICSharpProcedureElement : ICSharpElement
{
}

[Kind(DotNetAttributeValues.Kind.Method)]
public interface IDotNetMethodElement : IDotNetElement, INamedElement
{
}

public interface ICSharpMethodElement : IDotNetMethodElement, ICSharpProcedureElement
{
}

[Kind(DotNetAttributeValues.Kind.LocalFunction)]
public interface ICSharpLocalFunctionElement : ICSharpProcedureElement, INamedElement
{
}

[Kind(DotNetAttributeValues.Kind.Lambda)]
public interface ICSharpLambdaElement : ICSharpProcedureElement
{
}

[Kind(DotNetAttributeValues.Kind.Operation)]
public interface ICSharpOperationElement : ICSharpElement
{
}

[OperationKind(DotNetAttributeValues.OperationKind.Assignment)]
public interface ICSharpAssignmentElement : ICSharpOperationElement
{
}

[OperationKind(DotNetAttributeValues.OperationKind.ConditionalJump)]
public interface ICSharpConditionalJumpElement : ICSharpOperationElement
{
}

[OperationKind(DotNetAttributeValues.OperationKind.Call)]
public interface ICSharpCallElement : ICSharpOperationElement
{
}
