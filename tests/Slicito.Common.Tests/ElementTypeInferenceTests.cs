using FluentAssertions;

using Slicito.Abstractions.Facts;
using Slicito.Abstractions.Facts.Attributes;

namespace Slicito.Common.Tests;

[TestClass]
public class ElementTypeInferenceTests
{
    [Kind("Sample")]
    private interface ISampleElement : IElement
    {
    }

    [ElementAttribute("Purpose", "Test")]
    private interface ITestElement : ISampleElement
    {
    }

    [ElementAttribute("Purpose", "Fun")]
    private interface IFunElement : ISampleElement
    {
    }

    private interface IConflictedPurposeElement : ITestElement, IFunElement
    {
    }

    [TestMethod]
    public void IElement_IsInferredTo_Unrestricted_ElementType()
    {
        // Arrange
        var typeSystem = new TypeSystem();

        // Act
        var elementType = typeSystem.GetElementTypeFromInterface(typeof(IElement));

        // Assert
        elementType.Should().Be(typeSystem.GetUnrestrictedElementType());
    }

    [TestMethod]
    public void ISampleElement_IsInferredTo_SampleKind_ElementType()
    {
        // Arrange
        var typeSystem = new TypeSystem();

        // Act
        var elementType = typeSystem.GetElementTypeFromInterface(typeof(ISampleElement));

        // Assert
        elementType.Should().Be(typeSystem.GetElementType([("Kind", "Sample")]));
    }

    [TestMethod]
    public void ITestElement_IsInferredTo_SampleKind_And_TestPurpose_ElementType()
    {
        // Arrange
        var typeSystem = new TypeSystem();

        // Act
        var elementType = typeSystem.GetElementTypeFromInterface(typeof(ITestElement));

        // Assert
        elementType.Should().Be(typeSystem.GetElementType([("Kind", "Sample"), ("Purpose", "Test")]));
    }

    [TestMethod]
    public void Inferrence_Turns_Inheritance_Into_Containment()
    {
        // Arrange
        var typeSystem = new TypeSystem();

        // Act
        var anyElementType = typeSystem.GetElementTypeFromInterface(typeof(IElement));
        var sampleElementType = typeSystem.GetElementTypeFromInterface(typeof(ISampleElement));
        var testElementType = typeSystem.GetElementTypeFromInterface(typeof(ITestElement));
        var funElementType = typeSystem.GetElementTypeFromInterface(typeof(IFunElement));

        // Assert
        anyElementType.Value.IsStrictSupersetOf(sampleElementType.Value).Should().BeTrue();
        sampleElementType.Value.IsStrictSupersetOf(testElementType.Value).Should().BeTrue();
        sampleElementType.Value.IsStrictSupersetOf(funElementType.Value).Should().BeTrue();
        testElementType.Value.TryGetIntersection(funElementType.Value).Should().BeNull();
    }

    [TestMethod]
    public void IConflictedPurposeElement_Throws_ArgumentException()
    {
        // Arrange
        var typeSystem = new TypeSystem();

        // Act & Assert
        typeSystem.Invoking(ts => ts.GetElementTypeFromInterface(typeof(IConflictedPurposeElement)))
            .Should().Throw<ArgumentException>();
    }
}
