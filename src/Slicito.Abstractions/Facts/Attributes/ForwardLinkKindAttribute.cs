namespace Slicito.Abstractions.Facts.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ForwardLinkKindAttribute(string kind) : ForwardLinkAttributeAttribute(CommonAttributeNames.Kind, kind)
{
}
