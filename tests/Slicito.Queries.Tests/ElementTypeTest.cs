using FluentAssertions;

using Slicito.Abstractions.Queries;

namespace Slicito.Queries.Tests;

[TestClass]
public class ElementTypeTest
{
    [TestMethod]
    public void SameTypes_AreEqual()
    {
        // Arrange
        var typeSystem = new TypeSystem();

        // Act
        var kindABType1 = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A", "B"] } });
        var kindABType2 = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A", "B"] } });

        // Assert
        kindABType1.Should().BeEquivalentTo(kindABType2);
    }

    [TestMethod]
    public void DifferentTypes_AreNotEqual()
    {
        // Arrange
        var typeSystem = new TypeSystem();

        // Act
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var kindBType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["B"] } });

        // Assert
        kindAType.Should().NotBeEquivalentTo(kindBType);
    }

    [TestMethod]
    public void SmallestCommonSuperset_OfSameTypes_IsSameType()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });

        // Act
        var smallestCommonSuperset = kindAType.GetSmallestCommonSuperset(kindAType);

        // Assert
        smallestCommonSuperset.Should().BeEquivalentTo(kindAType);
    }

    [TestMethod]
    public void SmallestCommonSuperset_OfOrthogonalTypes_ContainsAllElements()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var colorBlueType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Color", ["Blue"] } });
        var anyType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>>());

        // Act
        var smallestCommonSuperset = kindAType.GetSmallestCommonSuperset(colorBlueType);

        // Assert
        smallestCommonSuperset.Should().BeEquivalentTo(anyType);
    }

    [TestMethod]
    public void SmallestCommonSuperset_OfNonoverlappingTypes_IsTheirUnion()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var kindBType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["B"] } });
        var kindABType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A", "B"] } });

        // Act
        var smallestCommonSuperset = kindAType.GetSmallestCommonSuperset(kindBType);

        // Assert
        smallestCommonSuperset.Should().BeEquivalentTo(kindABType);
    }

    [TestMethod]
    public void SmallestCommonSuperset_LosesPrecisionOfMoreSpecificType()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var kindB1Type = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["B"] }, { "BKind", ["1"] } });
        var kindABType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A", "B"] } });

        // Act
        var smallestCommonSuperset = kindAType.GetSmallestCommonSuperset(kindB1Type);

        // Assert
        smallestCommonSuperset.Should().BeEquivalentTo(kindABType);
    }

    [TestMethod]
    public void Union_OfSameTypes_IsSameType()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });

        // Act
        var union = kindAType.TryGetUnion(kindAType);

        // Assert
        union.Should().BeEquivalentTo(kindAType);
    }

    [TestMethod]
    public void Union_OfOrthogonalTypes_DoesNotExist()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var colorBlueType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Color", ["Blue"] } });

        // Act
        var union = kindAType.TryGetUnion(colorBlueType);

        // Assert
        union.Should().BeNull();
    }

    [TestMethod]
    public void Union_OfSimpleNonoverlappingTypes_IsCorrect()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var kindBType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["B"] } });
        var kindABType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A", "B"] } });

        // Act
        var union = kindAType.TryGetUnion(kindBType);

        // Assert
        union.Should().BeEquivalentTo(kindABType);
    }

    [TestMethod]
    public void Union_OfComplexNonoverlappingTypes_IsCorrect()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAColorBlueType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] }, { "Color", ["Blue"] } });
        var kindBColorBlueType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["B"] }, { "Color", ["Blue"] } });
        var kindABColorBlueType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A", "B"] }, { "Color", ["Blue"] } });

        // Act
        var union = kindAColorBlueType.TryGetUnion(kindBColorBlueType);

        // Assert
        union.Should().BeEquivalentTo(kindABColorBlueType);
    }

    [TestMethod]
    public void Union_OfUnmergeableTypes_DoesNotExist()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAColorBlueType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] }, { "Color", ["Blue"] } });
        var kindBColorRedType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["B"] }, { "Color", ["Red"] } });

        // Act
        var union = kindAColorBlueType.TryGetUnion(kindBColorRedType);

        // Assert
        union.Should().BeNull();
    }

    [TestMethod]
    public void Intersection_OfSameTypes_IsSameType()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });

        // Act
        var intersection = kindAType.TryGetIntersection(kindAType);

        // Assert
        intersection.Should().BeEquivalentTo(kindAType);
    }

    [TestMethod]
    public void Intersection_OfOrthogonalTypes_IsCorrect()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var colorBlueType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Color", ["Blue"] } });
        var kindAColorBlueType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] }, { "Color", ["Blue"] } });

        // Act
        var intersection = kindAType.TryGetIntersection(colorBlueType);

        // Assert
        intersection.Should().BeEquivalentTo(kindAColorBlueType);
    }

    [TestMethod]
    public void Intersection_OfSimpleNonoverlappingTypes_DoesNotExist()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var kindBType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["B"] } });

        // Act
        var intersection = kindAType.TryGetIntersection(kindBType);

        // Assert
        intersection.Should().BeNull();
    }

    [TestMethod]
    public void Intersection_OfComplexOverlappingTypes_IsCorrect()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindABColorBlueSmellFlowersType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A", "B"] }, { "Color", ["Blue"] }, { "Smell", ["Flowers"] } });
        var kindBCColorBlueYellowType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["B", "C"] }, { "Color", ["Blue", "Yellow"] } });
        var kindBcolorBlueSmellFlowersType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["B"] }, { "Color", ["Blue"] }, { "Smell", ["Flowers"] } });

        // Act
        var intersection = kindABColorBlueSmellFlowersType.TryGetIntersection(kindBCColorBlueYellowType);

        // Assert
        intersection.Should().BeEquivalentTo(kindBcolorBlueSmellFlowersType);
    }

    [TestMethod]
    public void IsSupersetOfOrEquals_SameType_IsTrue()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });

        // Act
        var isSupersetOf = kindAType.IsSupersetOfOrEquals(kindAType);

        // Assert
        isSupersetOf.Should().BeTrue();
    }

    [TestMethod]
    public void IsSupersetOfOrEquals_OrthogonalTypes_IsFalse()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var colorBlueType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Color", ["Blue"] } });

        // Act
        var isSupersetOf = kindAType.IsSupersetOfOrEquals(colorBlueType);

        // Assert
        isSupersetOf.Should().BeFalse();
    }

    [TestMethod]
    public void IsSupersetOfOrEquals_Superset_IsFalse()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var kindABType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A", "B"] } });

        // Act
        var isSupersetOf = kindAType.IsSupersetOfOrEquals(kindABType);

        // Assert
        isSupersetOf.Should().BeFalse();
    }

    [TestMethod]
    public void IsSupersetOfOrEquals_Subset_IsTrue()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindABType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A", "B"] } });
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });

        // Act
        var isSupersetOf = kindABType.IsSupersetOfOrEquals(kindAType);

        // Assert
        isSupersetOf.Should().BeTrue();
    }

    [TestMethod]
    public void IsSubsetOfOrEquals_SameType_IsTrue()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });

        // Act
        var isSubsetOf = kindAType.IsSubsetOfOrEquals(kindAType);

        // Assert
        isSubsetOf.Should().BeTrue();
    }

    [TestMethod]
    public void IsSubsetOfOrEquals_OrthogonalTypes_IsFalse()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var colorBlueType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Color", ["Blue"] } });

        // Act
        var isSubsetOf = kindAType.IsSubsetOfOrEquals(colorBlueType);

        // Assert
        isSubsetOf.Should().BeFalse();
    }

    [TestMethod]
    public void IsSubsetOfOrEquals_Superset_IsTrue()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var kindABType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A", "B"] } });

        // Act
        var isSubsetOf = kindAType.IsSubsetOfOrEquals(kindABType);

        // Assert
        isSubsetOf.Should().BeTrue();
    }

    [TestMethod]
    public void IsSubsetOfOrEquals_Subset_IsFalse()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindABType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A", "B"] } });
        var kindAType = typeSystem.GetElementType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });

        // Act
        var isSubsetOf = kindABType.IsSubsetOfOrEquals(kindAType);

        // Assert
        isSubsetOf.Should().BeFalse();
    }
}
