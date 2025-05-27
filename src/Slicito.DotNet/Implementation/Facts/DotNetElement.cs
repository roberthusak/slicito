using Slicito.Abstractions;
using Slicito.DotNet.Facts;

namespace Slicito.DotNet.Implementation.Facts;

internal abstract class DotNetElement(ElementId id) : IDotNetElement
{
    public string Runtime => DotNetAttributeValues.Runtime.DotNet;

    public ElementId Id { get; } = id;
}

internal abstract class CSharpElement(ElementId id) : DotNetElement(id), ICSharpElement
{
    public string Language => DotNetAttributeValues.Language.CSharp;
}

internal class SolutionElement(ElementId id, string name) : CSharpElement(id), ISolutionElement
{
    public string Name { get; } = name;
}

internal class CSharpProjectElement(ElementId id, string name, ProjectOutputKind outputKind) : CSharpElement(id), ICSharpProjectElement
{
    public string Name { get; } = name;

    public ProjectOutputKind OutputKind { get; } = outputKind;
}

internal class CSharpNamespaceElement(ElementId id, string name) : CSharpElement(id), ICSharpNamespaceElement
{
    public string Name { get; } = name;
}

internal class CSharpTypeElement(ElementId id, string name) : CSharpElement(id), ICSharpTypeElement
{
    public string Name { get; } = name;
}

internal class CSharpPropertyElement(ElementId id, string name) : CSharpElement(id), ICSharpPropertyElement
{
    public string Name { get; } = name;
}

internal class CSharpFieldElement(ElementId id, string name) : CSharpElement(id), ICSharpFieldElement
{
    public string Name { get; } = name;
}

internal class CSharpMethodElement(ElementId id, string name) : CSharpElement(id), ICSharpMethodElement
{
    public string Name { get; } = name;
}

internal abstract class CSharpOperationElement(ElementId id) : CSharpElement(id), ICSharpOperationElement
{
}

internal class CSharpAssignmentElement(ElementId id) : CSharpOperationElement(id), ICSharpAssignmentElement
{
}

internal class CSharpConditionalJumpElement(ElementId id) : CSharpOperationElement(id), ICSharpConditionalJumpElement
{
}

internal class CSharpCallElement(ElementId id) : CSharpOperationElement(id), ICSharpCallElement
{
}
