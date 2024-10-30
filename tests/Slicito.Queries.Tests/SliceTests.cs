using FluentAssertions;

using Slicito.Abstractions;

namespace Slicito.Queries.Tests;

[TestClass]
public class SliceTests
{
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
            .AddRootElements(new(kindAType), () => new([new(new("A1"))]))
            .AddRootElements(new(kindAType), () => new([new(new("A2"))]))
            .AddRootElements(new(kindBType), () => new([new(new("B1")), new(new("B2"))]))
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
        var kindAColorGreenType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] }, { "Color", ["Green"] } });
        var kindAColorBlueRedType = kindAColorBlueType.TryGetUnion(kindAColorRedType)!;
        var kindAColorRedGreenType = kindAColorRedType.TryGetUnion(kindAColorGreenType)!;

        // Act

        var slice = new SliceBuilder()
            .AddRootElements(new(kindAType), () => new([new(new("A1")), new(new("A2"))]))
            .AddRootElements(new(kindAColorBlueType), () => new([new(new("ABlue1"))]))
            .AddRootElements(new(kindAColorRedType), () => new([new(new("ARed1"))]))
            .AddRootElements(new(kindAColorGreenType), () => new([new(new("AGreen1"))]))
            .AddRootElements(new(kindAType), () => new(
            [
                new(new("ABlue2"), new(kindAColorBlueType)),
                new(new("ARed2"), new(kindAColorRedType)),
                new(new("AGreen2"), new(kindAColorGreenType))
            ]))
            .AddRootElements(new(kindAColorBlueRedType), () => new(
            [
                new(new("ABlue3"), new(kindAColorBlueType)),
                new(new("ARed3"), new(kindAColorRedType))
            ]))
            .BuildLazy();

        var aElementIds = await slice.GetRootElementIdsAsync(new(kindAType));
        var aBlueElementIds = await slice.GetRootElementIdsAsync(new(kindAColorBlueType));
        var aRedElementIds = await slice.GetRootElementIdsAsync(new(kindAColorRedType));
        var aBlueRedElementIds = await slice.GetRootElementIdsAsync(new(kindAColorBlueRedType));
        var aRedGreenElementIds = await slice.GetRootElementIdsAsync(new(kindAColorRedGreenType));

        // Assert
        aElementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(["A1", "A2", "ABlue1", "ARed1", "AGreen1", "ABlue2", "ARed2", "AGreen2", "ABlue3", "ARed3"]);
        aBlueElementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(["ABlue1", "ABlue2", "ABlue3"]);
        aRedElementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(["ARed1", "ARed2", "ARed3"]);
        aBlueRedElementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(["ABlue1", "ARed1", "ABlue2", "ARed2", "ABlue3", "ARed3"]);
        aRedGreenElementIds.Select(id => id.Value)
            .Should().BeEquivalentTo(["ARed1", "AGreen1", "ARed2", "AGreen2", "ARed3"]);
    }

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
        var builder = new SliceBuilder().AddElementAttribute(new(kindAType), "name", _ => new(""));

        // Assert
        builder.Invoking(b => b.AddElementAttribute(new(kindAType), "name", _ => new("")))
            .Should().Throw<InvalidOperationException>();
        builder.Invoking(b => b.AddElementAttribute(new(kindABType), "name", _ => new("")))
            .Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public async Task SliceBuilder_AddElementAttributes_Works()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var anyType = typeSystem.GetFactType(new Dictionary<string, IEnumerable<string>>());
        var kindAType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] } });
        var kindBType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["B"] } });
        var kindAColorBlueType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["A"] }, { "Color", ["Blue"] } });

        // Act

        var slice = new SliceBuilder()
            .AddRootElements(new(kindAType), () => new([new(new("A1"))]))
            .AddRootElements(new(kindBType), () => new([new(new("B1"))]))
            .AddRootElements(new(kindAColorBlueType), () => new([new(new("ABlue1"))]))
            .AddRootElements(new(anyType), () => new(
            [
                new(new("Any1")),
                new(new("A2"), new(kindAType)),
                new(new("B2"), new(kindBType)),
                new(new("ABlue2"), new(kindAColorBlueType))
            ]))
            .AddElementAttribute(new(kindAType), "name", id => new($"Mr. {id.Value} A"))
            .AddElementAttribute(new(kindBType), "name", id => new($"Mr. {id.Value} B"))
            .AddElementAttribute(new(anyType), "greeting", id => new($"Hello, {id.Value}!"))
            .BuildLazy();

        var nameProvider = slice.GetElementAttributeProviderAsyncCallback("name");
        var greetingProvider = slice.GetElementAttributeProviderAsyncCallback("greeting");

        // It's expected that the user will only use the element IDs that were previously obtained from the slice.
        var rootElementIds = (await slice.GetRootElementIdsAsync()).ToArray();

        var a1Name = await nameProvider(new("A1"));
        var a2Name = await nameProvider(new("A2"));
        var b1Name = await nameProvider(new("B1"));
        var b2Name = await nameProvider(new("B2"));
        var aBlue1Name = await nameProvider(new("ABlue1"));
        var aBlue2Name = await nameProvider(new("ABlue2"));

        var any1Greeting = await greetingProvider(new("Any1"));
        var a1Greeting = await greetingProvider(new("A1"));
        var a2Greeting = await greetingProvider(new("A2"));
        var b1Greeting = await greetingProvider(new("B1"));
        var b2Greeting = await greetingProvider(new("B2"));
        var aBlue1Greeting = await greetingProvider(new("ABlue1"));
        var aBlue2Greeting = await greetingProvider(new("ABlue2"));

        // Assert

        rootElementIds.Should().BeEquivalentTo(new[]
        {
            new ElementId("A1"),
            new ElementId("B1"),
            new ElementId("ABlue1"),
            new ElementId("Any1"),
            new ElementId("A2"),
            new ElementId("B2"),
            new ElementId("ABlue2")
        });

        await nameProvider.Invoking(async p => await p(new("NonExistent")))
            .Should().ThrowAsync<InvalidOperationException>();
        await nameProvider.Invoking(async p => await p(new("Any1")))
            .Should().ThrowAsync<InvalidOperationException>();

        a1Name.Should().Be("Mr. A1 A");
        a2Name.Should().Be("Mr. A2 A");
        b1Name.Should().Be("Mr. B1 B");
        b2Name.Should().Be("Mr. B2 B");
        aBlue1Name.Should().Be("Mr. ABlue1 A");
        aBlue2Name.Should().Be("Mr. ABlue2 A");

        any1Greeting.Should().Be("Hello, Any1!");
        a1Greeting.Should().Be("Hello, A1!");
        a2Greeting.Should().Be("Hello, A2!");
        b1Greeting.Should().Be("Hello, B1!");
        b2Greeting.Should().Be("Hello, B2!");
        aBlue1Greeting.Should().Be("Hello, ABlue1!");
        aBlue2Greeting.Should().Be("Hello, ABlue2!");
    }

    [TestMethod]
    public async Task SliceBuilder_AddLinks_LazyLinkExplorer_Works()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var anyType = typeSystem.GetFactType(new Dictionary<string, IEnumerable<string>>());
        var kindPointsToType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["PointsTo"] } });
        var kindPointsToColorBlueType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["PointsTo"] }, { "Color", ["Blue"] } });
        var kindPointsToColorRedType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["PointsTo"] }, { "Color", ["Red"] } });
        var kindPointsToColorGreenType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["PointsTo"] }, { "Color", ["Green"] } });
        var kindPointsToColorBlackType = typeSystem.GetFactType(
            new Dictionary<string, IEnumerable<string>> { { "Kind", ["PointsTo"] }, { "Color", ["Black"] } });
        var kindPointsToColorBlueRedType = kindPointsToColorBlueType.TryGetUnion(kindPointsToColorRedType)!;
        var kindPointsToColorRedGreenType = kindPointsToColorRedType.TryGetUnion(kindPointsToColorGreenType)!;

        // Act

        var slice = new SliceBuilder()
            .AddRootElements(new(anyType), () => new(
            [
                new(new("A")), new(new("B")), new(new("C")), new(new("D")),
            ]))
            .AddLinks(new(kindPointsToType), new(anyType), new(anyType), sourceId =>
            {
                return sourceId.Value switch
                {
                    "A" => new([new ISliceBuilder.LinkInfo(new(new("B")), new(kindPointsToColorBlueType))]),
                    "B" => new([new ISliceBuilder.LinkInfo(new(new("C")), new(kindPointsToColorRedType))]),
                    _ => new([])
                };
            })
            .AddLinks(new(kindPointsToType), new(anyType), new(anyType), sourceId =>
            {
                return sourceId.Value switch
                {
                    "C" => new([new ISliceBuilder.LinkInfo(new(new("D")), new(kindPointsToColorGreenType))]),
                    "D" => new([new ISliceBuilder.LinkInfo(new(new("A")))]),
                    _ => new([])
                };
            })
            .AddLinks(new(kindPointsToColorBlueType), new(anyType), new(anyType), sourceId =>
            {
                return sourceId.Value switch
                {
                    "B" => new(new ISliceBuilder.LinkInfo(new(new("C")))),
                    _ => new((ISliceBuilder.LinkInfo?)null)
                };
            })
            .AddLinks(new(kindPointsToColorRedType), new(anyType), new(anyType), sourceId =>
            {
                return sourceId.Value switch
                {
                    "C" => new([new ISliceBuilder.LinkInfo(new(new("D")))]),
                    _ => new([])
                };
            })
            .AddLinks(new(kindPointsToColorBlueRedType), new(anyType), new(anyType), sourceId =>
            {
                return sourceId.Value switch
                {
                    "C" => new([new ISliceBuilder.LinkInfo(new(new("D")), new(kindPointsToColorBlueType))]),
                    "D" => new([new ISliceBuilder.LinkInfo(new(new("A")), new(kindPointsToColorRedType))]),
                    _ => new([])
                };
            })
            .BuildLazy();
            
        var allLinksExplorer = slice.GetLinkExplorer(new(anyType));
        var pointsToLinksExplorer = slice.GetLinkExplorer(new(kindPointsToType));
        var pointsToColorBlueLinksExplorer = slice.GetLinkExplorer(new(kindPointsToColorBlueType));
        var pointsToColorRedLinksExplorer = slice.GetLinkExplorer(new(kindPointsToColorRedType));
        var pointsToColorBlackLinksExplorer = slice.GetLinkExplorer(new(kindPointsToColorBlackType));
        var pointsToColorRedGreenLinksExplorer = slice.GetLinkExplorer(new(kindPointsToColorRedGreenType));

        // It's expected that the user will only use the element IDs that were previously obtained from the slice.
        var rootElementIds = (await slice.GetRootElementIdsAsync()).ToArray();

        var aAllTargets = await allLinksExplorer.GetTargetElementIdsAsync(new("A"));
        var bAllTargets = await allLinksExplorer.GetTargetElementIdsAsync(new("B"));
        var cAllTargets = await allLinksExplorer.GetTargetElementIdsAsync(new("C"));
        var dAllTargets = await allLinksExplorer.GetTargetElementIdsAsync(new("D"));

        var aPointsToTargets = await pointsToLinksExplorer.GetTargetElementIdsAsync(new("A"));
        var bPointsToTargets = await pointsToLinksExplorer.GetTargetElementIdsAsync(new("B"));
        var cPointsToTargets = await pointsToLinksExplorer.GetTargetElementIdsAsync(new("C"));
        var dPointsToTargets = await pointsToLinksExplorer.GetTargetElementIdsAsync(new("D"));

        var aPointsToColorBlueTargets = await pointsToColorBlueLinksExplorer.GetTargetElementIdsAsync(new("A"));
        var bPointsToColorBlueTargets = await pointsToColorBlueLinksExplorer.GetTargetElementIdsAsync(new("B"));
        var cPointsToColorBlueTargets = await pointsToColorBlueLinksExplorer.GetTargetElementIdsAsync(new("C"));
        var dPointsToColorBlueTargets = await pointsToColorBlueLinksExplorer.GetTargetElementIdsAsync(new("D"));

        var aPointsToColorRedTargets = await pointsToColorRedLinksExplorer.GetTargetElementIdsAsync(new("A"));
        var bPointsToColorRedTargets = await pointsToColorRedLinksExplorer.GetTargetElementIdsAsync(new("B"));
        var cPointsToColorRedTargets = await pointsToColorRedLinksExplorer.GetTargetElementIdsAsync(new("C"));
        var dPointsToColorRedTargets = await pointsToColorRedLinksExplorer.GetTargetElementIdsAsync(new("D"));

        var aPointsToColorBlackTargets = await pointsToColorBlackLinksExplorer.GetTargetElementIdsAsync(new("A"));
        var bPointsToColorBlackTargets = await pointsToColorBlackLinksExplorer.GetTargetElementIdsAsync(new("B"));
        var cPointsToColorBlackTargets = await pointsToColorBlackLinksExplorer.GetTargetElementIdsAsync(new("C"));
        var dPointsToColorBlackTargets = await pointsToColorBlackLinksExplorer.GetTargetElementIdsAsync(new("D"));

        var aPointsToColorRedGreenTargets = await pointsToColorRedGreenLinksExplorer.GetTargetElementIdsAsync(new("A"));
        var bPointsToColorRedGreenTargets = await pointsToColorRedGreenLinksExplorer.GetTargetElementIdsAsync(new("B"));
        var cPointsToColorRedGreenTargets = await pointsToColorRedGreenLinksExplorer.GetTargetElementIdsAsync(new("C"));
        var dPointsToColorRedGreenTargets = await pointsToColorRedGreenLinksExplorer.GetTargetElementIdsAsync(new("D"));

        var aPointsToColorRedGreenTarget = await pointsToColorRedGreenLinksExplorer.TryGetTargetElementIdAsync(new("A"));
        var bPointsToColorRedGreenTarget = await pointsToColorRedGreenLinksExplorer.TryGetTargetElementIdAsync(new("B"));
        var dPointsToColorRedGreenTarget = await pointsToColorRedGreenLinksExplorer.TryGetTargetElementIdAsync(new("D"));

        // Assert

        rootElementIds.Select(id => id.Value).Should().BeEquivalentTo(["A", "B", "C", "D"]);

        aAllTargets.Select(id => id.Value).Should().BeEquivalentTo(["B"]);
        bAllTargets.Select(id => id.Value).Should().BeEquivalentTo(["C", "C"]);
        cAllTargets.Select(id => id.Value).Should().BeEquivalentTo(["D", "D", "D"]);
        dAllTargets.Select(id => id.Value).Should().BeEquivalentTo(["A", "A"]);

        aPointsToTargets.Select(id => id.Value).Should().BeEquivalentTo(["B"]);
        bPointsToTargets.Select(id => id.Value).Should().BeEquivalentTo(["C", "C"]);
        cPointsToTargets.Select(id => id.Value).Should().BeEquivalentTo(["D", "D", "D"]);
        dPointsToTargets.Select(id => id.Value).Should().BeEquivalentTo(["A", "A"]);

        aPointsToColorBlueTargets.Select(id => id.Value).Should().BeEquivalentTo(["B"]);
        bPointsToColorBlueTargets.Select(id => id.Value).Should().BeEquivalentTo(["C"]);
        cPointsToColorBlueTargets.Select(id => id.Value).Should().BeEquivalentTo(["D"]);
        dPointsToColorBlueTargets.Should().BeEmpty();

        aPointsToColorRedTargets.Should().BeEmpty();
        bPointsToColorRedTargets.Select(id => id.Value).Should().BeEquivalentTo(["C"]);
        cPointsToColorRedTargets.Select(id => id.Value).Should().BeEquivalentTo(["D"]);
        dPointsToColorRedTargets.Select(id => id.Value).Should().BeEquivalentTo(["A"]);

        aPointsToColorBlackTargets.Should().BeEmpty();
        bPointsToColorBlackTargets.Should().BeEmpty();
        cPointsToColorBlackTargets.Should().BeEmpty();
        dPointsToColorBlackTargets.Should().BeEmpty();

        aPointsToColorRedGreenTargets.Should().BeEmpty();
        bPointsToColorRedGreenTargets.Select(id => id.Value).Should().BeEquivalentTo(["C"]);
        cPointsToColorRedGreenTargets.Select(id => id.Value).Should().BeEquivalentTo(["D", "D"]);
        dPointsToColorRedGreenTargets.Select(id => id.Value).Should().BeEquivalentTo(["A"]);

        aPointsToColorRedGreenTarget.HasValue.Should().BeFalse();
        bPointsToColorRedGreenTarget.Should().Be(new ElementId("C"));
        await pointsToColorRedGreenLinksExplorer.Invoking(async e => await e.TryGetTargetElementIdAsync(new("C")))
            .Should().ThrowAsync<InvalidOperationException>();
        dPointsToColorRedGreenTarget.Should().Be(new ElementId("A"));
    }
}
