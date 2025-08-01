namespace Slicito.Abstractions.Facts.Attributes;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public class RuntimeAttribute(string runtime) : ElementAttributeAttribute(CommonAttributeNames.Runtime, runtime)
{
}
