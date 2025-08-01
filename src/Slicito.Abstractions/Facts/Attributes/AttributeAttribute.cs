namespace Slicito.Abstractions.Facts.Attributes;

[AttributeUsage(AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true)]
public class AttributeAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
