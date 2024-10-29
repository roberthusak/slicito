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
            .AddRootElements(new(kindAType), () => new ValueTask<IEnumerable<ISliceBuilder.ElementInfo>>([new(new("A1"))]))
            .AddRootElements(new(kindAType), () => new ValueTask<IEnumerable<ISliceBuilder.ElementInfo>>([new(new("A2"))]))
            .AddRootElements(new(kindBType), () => new ValueTask<IEnumerable<ISliceBuilder.ElementInfo>>([new(new("B1")), new(new("B2"))]))
            .BuildLazy();

        var aElementIds = await slice.GetRootElementIdsAsync(new(kindAType));
        var bElementIds = await slice.GetRootElementIdsAsync(new(kindBType));
        var abElementIds = await slice.GetRootElementIdsAsync(new(kindABType));

        // Assert
        aElementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(["A1", "A2"]);
        bElementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(["B1", "B2"]);
        abElementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(["A1", "A2", "B1", "B2"]);
    }

    [TestMethod]
    public async Task SliceBuilder_AddRootElements_DetailedFilter_Works()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var kindAColorBlueType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] }, { "Color", ["Blue"] } });
        var kindAColorRedType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] }, { "Color", ["Red"] } });
        var kindAColorBlueRedType = kindAColorBlueType.TryGetUnion(kindAColorRedType)!;

        // Act

        var slice = new SliceBuilder()
            .AddRootElements(new(kindAType), () => new ValueTask<IEnumerable<ISliceBuilder.ElementInfo>>([new(new("A1")), new(new("A2"))]))
            .AddRootElements(new(kindAColorBlueType), () => new ValueTask<IEnumerable<ISliceBuilder.ElementInfo>>([new(new("ABlue1"))]))
            .AddRootElements(new(kindAColorRedType), () => new ValueTask<IEnumerable<ISliceBuilder.ElementInfo>>([new(new("ARed1"))]))
            .AddRootElements(new(kindAType), () => new ValueTask<IEnumerable<ISliceBuilder.ElementInfo>>(
            [
                new(new("ABlue2"), new(kindAColorBlueType)),
                new(new("ARed2"), new(kindAColorRedType))
            ]))
            .BuildLazy();

        var aElementIds = await slice.GetRootElementIdsAsync(new(kindAType));
        var aBlueElementIds = await slice.GetRootElementIdsAsync(new(kindAColorBlueType));
        var aRedElementIds = await slice.GetRootElementIdsAsync(new(kindAColorRedType));
        var aBlueRedElementIds = await slice.GetRootElementIdsAsync(new(kindAColorBlueRedType));

        // Assert
        aElementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(["A1", "A2", "ABlue1", "ARed1", "ABlue2", "ARed2"]);
        aBlueElementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(["ABlue1", "ABlue2"]);
        aRedElementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(["ARed1", "ARed2"]);
        aBlueRedElementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(["ABlue1", "ARed1", "ABlue2", "ARed2"]);
    }
}
