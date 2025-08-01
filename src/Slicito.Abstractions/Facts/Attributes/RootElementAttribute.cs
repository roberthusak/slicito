namespace Slicito.Abstractions.Facts.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class RootElementAttribute(Type elementType) : Attribute
{
    public Type ElementType { get; } = elementType;
}
