namespace Slicito.Abstractions.Facts.Attributes;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public class LanguageAttribute(string language) : ElementAttributeAttribute(CommonAttributeNames.Language, language)
{
}
