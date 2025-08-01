namespace Slicito.Abstractions.Facts.Attributes;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public class KindAttribute(string kind) : ElementAttributeAttribute(CommonAttributeNames.Kind, kind)
{
}
