namespace Slicito.Abstractions.Facts.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class BackwardLinkKindAttribute(string kind) : BackwardLinkAttributeAttribute(CommonAttributeNames.Kind, kind)
{
}
