namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
public sealed class NotNullWhenAttribute : Attribute
{
    /// <summary>
    ///     Gets the return value condition.
    ///     If the method returns this value, the associated parameter will not be <see langword="null"/>.
    /// </summary>
    public bool ReturnValue { get; }


    /// <summary>
    ///     Initializes the attribute with the specified return value condition.
    /// </summary>
    /// <param name="returnValue">
    ///     The return value condition.
    ///     If the method returns this value, the associated parameter will not be <see langword="null"/>.
    /// </param>
    public NotNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }
}
