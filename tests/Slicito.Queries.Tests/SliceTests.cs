using FluentAssertions;

using Slicito.Abstractions;

namespace Slicito.Queries.Tests;

[TestClass]
public class SliceTests
{
    [TestMethod]
    public void SliceBuilder_AddElementAttributes_OverlappingTypes_Throws()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var kindABType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A", "B"] } });

        // Act
        var builder = new SliceBuilder().AddElementAttribute(new(kindAType), "name", _ => new ValueTask<string>(""));

        // Assert
        builder.Invoking(b => b.AddElementAttribute(new(kindAType), "name", _ => new ValueTask<string>("")))
            .Should().Throw<InvalidOperationException>();
        builder.Invoking(b => b.AddElementAttribute(new(kindABType), "name", _ => new ValueTask<string>("")))
            .Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public async Task SliceBuilder_AddRootElements_MultipleTimes_Merges()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetFactType(
                       new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var kindBType = typeSystem.GetFactType(
                       new Dictionary<string, IEnumerable<string>> { { "Kind", ["B"] } });
        var kindABType = kindAType.TryGetUnion(kindBType)!;

        // Act

        var slice = new SliceBuilder()
            .AddRootElements(new(kindAType), () => new ValueTask<IEnumerable<ElementId>>([new("1")]))
            .AddRootElements(new(kindAType), () => new ValueTask<IEnumerable<ElementId>>([new("2")]))
            .AddRootElements(new(kindBType), () => new ValueTask<IEnumerable<ElementId>>([new("3"), new("4")]))
            .BuildLazy();

        var aElementIds = await slice.GetRootElementIdsAsync(new(kindAType));
        var bElementIds = await slice.GetRootElementIdsAsync(new(kindBType));
        var abElementIds = await slice.GetRootElementIdsAsync(new(kindABType));

        // Assert
        aElementIds.Should().BeEquivalentTo([new ElementId("1"), new ElementId("2")]);
        bElementIds.Should().BeEquivalentTo([new ElementId("3"), new ElementId("4")]);
        abElementIds.Should().BeEquivalentTo([new ElementId("1"), new ElementId("2"), new ElementId("3"), new ElementId("4")]);
    }
}
