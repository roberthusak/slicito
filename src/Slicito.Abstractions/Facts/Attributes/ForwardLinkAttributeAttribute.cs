namespace Slicito.Abstractions.Facts.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ForwardLinkAttributeAttribute(string name, string value) : Attribute
{
    public string Name { get; } = name;
    public string Value { get; } = value;
}
