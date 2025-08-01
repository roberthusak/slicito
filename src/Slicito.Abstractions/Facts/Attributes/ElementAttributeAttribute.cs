namespace Slicito.Abstractions.Facts.Attributes;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
public class ElementAttributeAttribute(string name, string value) : Attribute
{
    public string Name { get; } = name;
    public string Value { get; } = value;
}
