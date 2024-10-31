using FluentAssertions;

using Slicito.Abstractions;
using Slicito.Abstractions.Queries;

namespace Slicito.Queries.Tests;

[TestClass]
public class SliceTests
{
    [TestMethod]
    public async Task SliceBuilder_AddRootElements_MultipleTimes_Merges()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType([("Kind", "A")]);
        var kindBType = typeSystem.GetElementType([("Kind", "B")]);
        var kindABType = (kindAType | kindBType)!;

        // Act

        var slice = new SliceBuilder()
            .AddRootElements(kindAType, () => new([new(new("A1"))]))
            .AddRootElements(kindAType, () => new([new(new("A2"))]))
            .AddRootElements(kindBType, () => new([new(new("B1")), new(new("B2"))]))
            .BuildLazy();

        var aElements = await slice.GetRootElementsAsync(kindAType);
        var bElements = await slice.GetRootElementsAsync(kindBType);
        var abElements = await slice.GetRootElementsAsync(kindABType);

        var a1 = new ElementInfo(new("A1"), kindAType);
        var a2 = new ElementInfo(new("A2"), kindAType);
        var b1 = new ElementInfo(new("B1"), kindBType);
        var b2 = new ElementInfo(new("B2"), kindBType);

        // Assert
        aElements.Should().BeEquivalentTo([a1, a2]);
        bElements.Should().BeEquivalentTo([b1, b2]);
        abElements.Should().BeEquivalentTo([a1, a2, b1, b2]);
    }

    [TestMethod]
    public async Task SliceBuilder_AddRootElements_DetailedFilter_Works()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType([("Kind", "A")]);
        var kindAColorBlueType = typeSystem.GetElementType([("Kind", "A"), ("Color", "Blue")]);
        var kindAColorRedType = typeSystem.GetElementType([("Kind", "A"), ("Color", "Red")]);
        var kindAColorGreenType = typeSystem.GetElementType([("Kind", "A"), ("Color", "Green")]);
        var kindAColorBlueRedType = (kindAColorBlueType | kindAColorRedType)!.Value;
        var kindAColorRedGreenType = (kindAColorRedType | kindAColorGreenType)!.Value;

        // Act

        var slice = new SliceBuilder()
            .AddRootElements(kindAType, () => new([new(new("A1")), new(new("A2"))]))
            .AddRootElements(kindAColorBlueType, () => new([new(new("ABlue1"))]))
            .AddRootElements(kindAColorRedType, () => new([new(new("ARed1"))]))
            .AddRootElements(kindAColorGreenType, () => new([new(new("AGreen1"))]))
            .AddRootElements(kindAType, () => new(
            [
                new(new("ABlue2"), kindAColorBlueType),
                new(new("ARed2"), kindAColorRedType),
                new(new("AGreen2"), kindAColorGreenType),
            ]))
            .AddRootElements(kindAColorBlueRedType, () => new(
            [
                new(new("ABlue3"), kindAColorBlueType),
                new(new("ARed3"), kindAColorRedType),
            ]))
            .BuildLazy();

        var aElements = await slice.GetRootElementsAsync(kindAType);
        var aBlueElements = await slice.GetRootElementsAsync(kindAColorBlueType);
        var aRedElements = await slice.GetRootElementsAsync(kindAColorRedType);
        var aBlueRedElements = await slice.GetRootElementsAsync(kindAColorBlueRedType);
        var aRedGreenElements = await slice.GetRootElementsAsync(kindAColorRedGreenType);

        var a1 = new ElementInfo(new("A1"), kindAType);
        var a2 = new ElementInfo(new("A2"), kindAType);
        var aBlue1 = new ElementInfo(new("ABlue1"), kindAColorBlueType);
        var aBlue2 = new ElementInfo(new("ABlue2"), kindAColorBlueType);
        var aBlue3 = new ElementInfo(new("ABlue3"), kindAColorBlueType);
        var aRed1 = new ElementInfo(new("ARed1"), kindAColorRedType);
        var aRed2 = new ElementInfo(new("ARed2"), kindAColorRedType);
        var aRed3 = new ElementInfo(new("ARed3"), kindAColorRedType);
        var aGreen1 = new ElementInfo(new("AGreen1"), kindAColorGreenType);
        var aGreen2 = new ElementInfo(new("AGreen2"), kindAColorGreenType);

        // Assert
        aElements.Should().BeEquivalentTo([a1, a2, aBlue1, aRed1, aGreen1, aBlue2, aRed2, aGreen2, aBlue3, aRed3]);
        aBlueElements.Should().BeEquivalentTo([aBlue1, aBlue2, aBlue3]);
        aRedElements.Should().BeEquivalentTo([aRed1, aRed2, aRed3]);
        aBlueRedElements.Should().BeEquivalentTo([aBlue1, aRed1, aBlue2, aRed2, aBlue3, aRed3]);
        aRedGreenElements.Should().BeEquivalentTo([aRed1, aGreen1, aRed2, aGreen2, aRed3]);
    }

    [TestMethod]
    public void SliceBuilder_AddElementAttributes_OverlappingTypes_Throws()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var kindAType = typeSystem.GetElementType([("Kind", "A")]);
        var kindABType = typeSystem.GetElementType([("Kind", ["A", "B"])]);

        // Act
        var builder = new SliceBuilder().AddElementAttribute(kindAType, "name", _ => new(""));

        // Assert
        builder.Invoking(b => b.AddElementAttribute(kindAType, "name", _ => new("")))
            .Should().Throw<InvalidOperationException>();
        builder.Invoking(b => b.AddElementAttribute(kindABType, "name", _ => new("")))
            .Should().Throw<InvalidOperationException>();
    }

    [TestMethod]
    public async Task SliceBuilder_AddElementAttributes_RootElements_Works()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var anyType = typeSystem.GetUnrestrictedElementType();
        var kindAType = typeSystem.GetElementType([("Kind", "A")]);
        var kindBType = typeSystem.GetElementType([("Kind", "B")]);
        var kindAColorBlueType = typeSystem.GetElementType([("Kind", "A"), ("Color", "Blue")]);

        // Act

        var slice = new SliceBuilder()
            .AddRootElements(kindAType, () => new([new(new("A1"))]))
            .AddRootElements(kindBType, () => new([new(new("B1"))]))
            .AddRootElements(kindAColorBlueType, () => new([new(new("ABlue1"))]))
            .AddRootElements(anyType, () => new(
            [
                new(new("Any1")),
                new(new("A2"), kindAType),
                new(new("B2"), kindBType),
                new(new("ABlue2"), kindAColorBlueType),
            ]))
            .AddElementAttribute(kindAType, "name", id => new($"Mr. {id.Value} A"))
            .AddElementAttribute(kindBType, "name", id => new($"Mr. {id.Value} B"))
            .AddElementAttribute(anyType, "greeting", id => new($"Hello, {id.Value}!"))
            .BuildLazy();

        var nameProvider = slice.GetElementAttributeProviderAsyncCallback("name");
        var greetingProvider = slice.GetElementAttributeProviderAsyncCallback("greeting");

        // It's expected that the user will only use the element IDs that were previously obtained from the slice.
        var rootElements = (await slice.GetRootElementsAsync()).ToArray();

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

        rootElements.Should().BeEquivalentTo(new[]
        {
            new ElementInfo(new("A1"), kindAType),
            new ElementInfo(new("B1"), kindBType),
            new ElementInfo(new("ABlue1"), kindAColorBlueType),
            new ElementInfo(new("Any1"), anyType),
            new ElementInfo(new("A2"), kindAType),
            new ElementInfo(new("B2"), kindBType),
            new ElementInfo(new("ABlue2"), kindAColorBlueType),
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
    public async Task SliceBuilder_AddElementAttributes_NestedElements_Works()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var anyType = typeSystem.GetUnrestrictedElementType();
        var kindAType = typeSystem.GetElementType([("Kind", "A")]);
        var kindBType = typeSystem.GetElementType([("Kind", "B")]);
        var kindABType = (kindAType | kindBType)!.Value;
        var kindAColorBlueType = typeSystem.GetElementType([("Kind", "A"), ("Color", "Blue")]);
        var kindContainsType = typeSystem.GetLinkType([("Kind", "Contains")]);

        // Act

        var slice = new SliceBuilder()
            .AddRootElements(kindAType, () => new([new(new("A1Root")), new(new("A2Root"))]))
            .AddHierarchyLinks(kindContainsType, kindAType, kindABType, sourceId =>
            {
                return sourceId.Value switch
                {
                    "A1Root" => new([new ISliceBuilder.PartialLinkInfo(new(new("A1NestedA3"), kindAType))]),
                    "A2Root" => new([new ISliceBuilder.PartialLinkInfo(new(new("A2NestedB1"), kindBType))]),
                    _ => new([])
                };
            })
            .AddHierarchyLinks(kindContainsType, kindAType, kindAType, sourceId =>
            {
                return sourceId.Value switch
                {
                    "A1Root" => new([new ISliceBuilder.PartialLinkInfo(new(new("A1NestedA4")))]),
                    _ => new([])
                };
            })
            .AddHierarchyLinks(kindContainsType, kindAType, kindBType, sourceId =>
            {
                return sourceId.Value switch
                {
                    "A2Root" => new([new ISliceBuilder.PartialLinkInfo(new(new("A2NestedB2"), kindAColorBlueType))]),
                    _ => new([])
                };
            })
            .AddElementAttribute(kindAType, "name", id => new($"Mr. {id.Value} A"))
            .AddElementAttribute(kindBType, "name", id => new($"Mr. {id.Value} B"))
            .AddElementAttribute(anyType, "greeting", id => new($"Hello, {id.Value}!"))
            .BuildLazy();

        var containsLinksExplorer = slice.GetLinkExplorer(kindContainsType);

        // It's expected that the user will only use the element IDs that were previously obtained from the slice.
        var rootElements = (await slice.GetRootElementsAsync()).ToArray();

        var nestedElements = new List<ElementInfo>();
        foreach (var rootElement in rootElements)
        {
            nestedElements.AddRange(await containsLinksExplorer.GetTargetElementsAsync(rootElement.Id));
        }

        var nameProvider = slice.GetElementAttributeProviderAsyncCallback("name");
        var greetingProvider = slice.GetElementAttributeProviderAsyncCallback("greeting");

        var a1RootName = await nameProvider(new("A1Root"));
        var a2RootName = await nameProvider(new("A2Root"));
        var a1NestedA3Name = await nameProvider(new("A1NestedA3"));
        var a2NestedB1Name = await nameProvider(new("A2NestedB1"));
        var a1NestedA4Name = await nameProvider(new("A1NestedA4"));
        var a2NestedB2Name = await nameProvider(new("A2NestedB2"));

        var a1RootGreeting = await greetingProvider(new("A1Root"));
        var a2RootGreeting = await greetingProvider(new("A2Root"));
        var a1NestedA3Greeting = await greetingProvider(new("A1NestedA3"));
        var a2NestedB1Greeting = await greetingProvider(new("A2NestedB1"));
        var a1NestedA4Greeting = await greetingProvider(new("A1NestedA4"));
        var a2NestedB2Greeting = await greetingProvider(new("A2NestedB2"));

        var a1Root = new ElementInfo(new("A1Root"), kindAType);
        var a2Root = new ElementInfo(new("A2Root"), kindAType);
        var a1NestedA3 = new ElementInfo(new("A1NestedA3"), kindAType);
        var a2NestedB1 = new ElementInfo(new("A2NestedB1"), kindBType);
        var a1NestedA4 = new ElementInfo(new("A1NestedA4"), kindAType);
        var a2NestedB2 = new ElementInfo(new("A2NestedB2"), kindAColorBlueType);

        // Assert

        rootElements.Should().BeEquivalentTo([a1Root, a2Root]);

        nestedElements.Should().BeEquivalentTo([a1NestedA3, a2NestedB1, a1NestedA4, a2NestedB2]);

        a1RootName.Should().Be("Mr. A1Root A");
        a2RootName.Should().Be("Mr. A2Root A");
        a1NestedA3Name.Should().Be("Mr. A1NestedA3 A");
        a2NestedB1Name.Should().Be("Mr. A2NestedB1 B");
        a1NestedA4Name.Should().Be("Mr. A1NestedA4 A");
        a2NestedB2Name.Should().Be("Mr. A2NestedB2 A");

        a1RootGreeting.Should().Be("Hello, A1Root!");
        a2RootGreeting.Should().Be("Hello, A2Root!");
        a1NestedA3Greeting.Should().Be("Hello, A1NestedA3!");
        a2NestedB1Greeting.Should().Be("Hello, A2NestedB1!");
        a1NestedA4Greeting.Should().Be("Hello, A1NestedA4!");
        a2NestedB2Greeting.Should().Be("Hello, A2NestedB2!");
    }

    [TestMethod]
    public async Task SliceBuilder_AddLinks_LazyLinkExplorer_Works()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var anyElementType = typeSystem.GetUnrestrictedElementType();
        var anyLinkType = typeSystem.GetUnrestrictedLinkType();
        var kindPointsToType = typeSystem.GetLinkType([("Kind", "PointsTo")]);
        var kindPointsToColorBlueType = typeSystem.GetLinkType([("Kind", "PointsTo"), ("Color", "Blue")]);
        var kindPointsToColorRedType = typeSystem.GetLinkType([("Kind", "PointsTo"), ("Color", "Red")]);
        var kindPointsToColorGreenType = typeSystem.GetLinkType([("Kind", "PointsTo"), ("Color", "Green")]);
        var kindPointsToColorBlackType = typeSystem.GetLinkType([("Kind", "PointsTo"), ("Color", "Black")]);
        var kindPointsToColorBlueRedType = (kindPointsToColorBlueType | kindPointsToColorRedType)!.Value;
        var kindPointsToColorRedGreenType = (kindPointsToColorRedType | kindPointsToColorGreenType)!.Value;

        // Act

        var slice = new SliceBuilder()
            .AddRootElements(anyElementType, () => new(
            [
                new(new("A")), new(new("B")), new(new("C")), new(new("D")),
            ]))
            .AddLinks(kindPointsToType, anyElementType, anyElementType, sourceId =>
            {
                return sourceId.Value switch
                {
                    "A" => new([new ISliceBuilder.PartialLinkInfo(new(new("B")), kindPointsToColorBlueType)]),
                    "B" => new([new ISliceBuilder.PartialLinkInfo(new(new("C")), kindPointsToColorRedType)]),
                    _ => new([])
                };
            })
            .AddLinks(kindPointsToType, anyElementType, anyElementType, sourceId =>
            {
                return sourceId.Value switch
                {
                    "C" => new([new ISliceBuilder.PartialLinkInfo(new(new("D")), kindPointsToColorGreenType)]),
                    "D" => new([new ISliceBuilder.PartialLinkInfo(new(new("A")))]),
                    _ => new([])
                };
            })
            .AddLinks(kindPointsToColorBlueType, anyElementType, anyElementType, sourceId =>
            {
                return sourceId.Value switch
                {
                    "B" => new(new ISliceBuilder.PartialLinkInfo(new(new("C")))),
                    _ => new((ISliceBuilder.PartialLinkInfo?)null)
                };
            })
            .AddLinks(kindPointsToColorRedType, anyElementType, anyElementType, sourceId =>
            {
                return sourceId.Value switch
                {
                    "C" => new([new ISliceBuilder.PartialLinkInfo(new(new("D")))]),
                    _ => new([])
                };
            })
            .AddLinks(kindPointsToColorBlueRedType, anyElementType, anyElementType, sourceId =>
            {
                return sourceId.Value switch
                {
                    "C" => new([new ISliceBuilder.PartialLinkInfo(new(new("D")), kindPointsToColorBlueType)]),
                    "D" => new([new ISliceBuilder.PartialLinkInfo(new(new("A")), kindPointsToColorRedType)]),
                    _ => new([])
                };
            })
            .BuildLazy();
            
        var allLinksExplorer = slice.GetLinkExplorer(anyLinkType);
        var pointsToLinksExplorer = slice.GetLinkExplorer(kindPointsToType);
        var pointsToColorBlueLinksExplorer = slice.GetLinkExplorer(kindPointsToColorBlueType);
        var pointsToColorRedLinksExplorer = slice.GetLinkExplorer(kindPointsToColorRedType);
        var pointsToColorBlackLinksExplorer = slice.GetLinkExplorer(kindPointsToColorBlackType);
        var pointsToColorRedGreenLinksExplorer = slice.GetLinkExplorer(kindPointsToColorRedGreenType);

        // It's expected that the user will only use the element IDs that were previously obtained from the slice.
        var rootElementIds = (await slice.GetRootElementsAsync()).ToArray();

        var aAllTargets = await allLinksExplorer.GetTargetElementsAsync(new("A"));
        var bAllTargets = await allLinksExplorer.GetTargetElementsAsync(new("B"));
        var cAllTargets = await allLinksExplorer.GetTargetElementsAsync(new("C"));
        var dAllTargets = await allLinksExplorer.GetTargetElementsAsync(new("D"));

        var aPointsToTargets = await pointsToLinksExplorer.GetTargetElementsAsync(new("A"));
        var bPointsToTargets = await pointsToLinksExplorer.GetTargetElementsAsync(new("B"));
        var cPointsToTargets = await pointsToLinksExplorer.GetTargetElementsAsync(new("C"));
        var dPointsToTargets = await pointsToLinksExplorer.GetTargetElementsAsync(new("D"));

        var aPointsToColorBlueTargets = await pointsToColorBlueLinksExplorer.GetTargetElementsAsync(new("A"));
        var bPointsToColorBlueTargets = await pointsToColorBlueLinksExplorer.GetTargetElementsAsync(new("B"));
        var cPointsToColorBlueTargets = await pointsToColorBlueLinksExplorer.GetTargetElementsAsync(new("C"));
        var dPointsToColorBlueTargets = await pointsToColorBlueLinksExplorer.GetTargetElementsAsync(new("D"));

        var aPointsToColorRedTargets = await pointsToColorRedLinksExplorer.GetTargetElementsAsync(new("A"));
        var bPointsToColorRedTargets = await pointsToColorRedLinksExplorer.GetTargetElementsAsync(new("B"));
        var cPointsToColorRedTargets = await pointsToColorRedLinksExplorer.GetTargetElementsAsync(new("C"));
        var dPointsToColorRedTargets = await pointsToColorRedLinksExplorer.GetTargetElementsAsync(new("D"));

        var aPointsToColorBlackTargets = await pointsToColorBlackLinksExplorer.GetTargetElementsAsync(new("A"));
        var bPointsToColorBlackTargets = await pointsToColorBlackLinksExplorer.GetTargetElementsAsync(new("B"));
        var cPointsToColorBlackTargets = await pointsToColorBlackLinksExplorer.GetTargetElementsAsync(new("C"));
        var dPointsToColorBlackTargets = await pointsToColorBlackLinksExplorer.GetTargetElementsAsync(new("D"));

        var aPointsToColorRedGreenTargets = await pointsToColorRedGreenLinksExplorer.GetTargetElementsAsync(new("A"));
        var bPointsToColorRedGreenTargets = await pointsToColorRedGreenLinksExplorer.GetTargetElementsAsync(new("B"));
        var cPointsToColorRedGreenTargets = await pointsToColorRedGreenLinksExplorer.GetTargetElementsAsync(new("C"));
        var dPointsToColorRedGreenTargets = await pointsToColorRedGreenLinksExplorer.GetTargetElementsAsync(new("D"));

        var aPointsToColorRedGreenTarget = await pointsToColorRedGreenLinksExplorer.TryGetTargetElementAsync(new("A"));
        var bPointsToColorRedGreenTarget = await pointsToColorRedGreenLinksExplorer.TryGetTargetElementAsync(new("B"));
        var dPointsToColorRedGreenTarget = await pointsToColorRedGreenLinksExplorer.TryGetTargetElementAsync(new("D"));

        var a = new ElementInfo(new("A"), anyElementType);
        var b = new ElementInfo(new("B"), anyElementType);
        var c = new ElementInfo(new("C"), anyElementType);
        var d = new ElementInfo(new("D"), anyElementType);

        // Assert

        rootElementIds.Should().BeEquivalentTo([a, b, c, d]);

        aAllTargets.Should().BeEquivalentTo([b]);
        bAllTargets.Should().BeEquivalentTo([c, c]);
        cAllTargets.Should().BeEquivalentTo([d, d, d]);
        dAllTargets.Should().BeEquivalentTo([a, a]);

        aPointsToTargets.Should().BeEquivalentTo([b]);
        bPointsToTargets.Should().BeEquivalentTo([c, c]);
        cPointsToTargets.Should().BeEquivalentTo([d, d, d]);
        dPointsToTargets.Should().BeEquivalentTo([a, a]);

        aPointsToColorBlueTargets.Should().BeEquivalentTo([b]);
        bPointsToColorBlueTargets.Should().BeEquivalentTo([c]);
        cPointsToColorBlueTargets.Should().BeEquivalentTo([d]);
        dPointsToColorBlueTargets.Should().BeEmpty();

        aPointsToColorRedTargets.Should().BeEmpty();
        bPointsToColorRedTargets.Should().BeEquivalentTo([c]);
        cPointsToColorRedTargets.Should().BeEquivalentTo([d]);
        dPointsToColorRedTargets.Should().BeEquivalentTo([a]);

        aPointsToColorBlackTargets.Should().BeEmpty();
        bPointsToColorBlackTargets.Should().BeEmpty();
        cPointsToColorBlackTargets.Should().BeEmpty();
        dPointsToColorBlackTargets.Should().BeEmpty();

        aPointsToColorRedGreenTargets.Should().BeEmpty();
        bPointsToColorRedGreenTargets.Should().BeEquivalentTo([c]);
        cPointsToColorRedGreenTargets.Should().BeEquivalentTo([d, d]);
        dPointsToColorRedGreenTargets.Should().BeEquivalentTo([a]);

        aPointsToColorRedGreenTarget.HasValue.Should().BeFalse();
        bPointsToColorRedGreenTarget.Should().Be(c);
        await pointsToColorRedGreenLinksExplorer.Invoking(async e => await e.TryGetTargetElementAsync(new("C")))
            .Should().ThrowAsync<InvalidOperationException>();
        dPointsToColorRedGreenTarget.Should().Be(a);
    }

    [TestMethod]
    public void SliceBuilder_Schema_IsCorrect()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var anyType = typeSystem.GetUnrestrictedElementType();
        var kindContainsType = typeSystem.GetLinkType([("Kind", "Contains")]);
        var kindPointsToType = typeSystem.GetLinkType([("Kind", "PointsTo")]);
        var kindPointsToColorBlueType = typeSystem.GetLinkType([("Kind", "PointsTo"), ("Color", "Blue")]);
        var kindAType = typeSystem.GetElementType([("Kind", "A")]);
        var kindAColorBlueType = typeSystem.GetElementType([("Kind", "A"), ("Color", "Blue")]);
        var kindBType = typeSystem.GetElementType([("Kind", "B")]);
        var kindABType = (kindAType | kindBType)!.Value;

        // Act
        var schema = new SliceBuilder()
            .AddRootElements(anyType, () => new([]))
            .AddRootElements(kindAType, () => new([]))
            .AddElementAttribute(kindAColorBlueType, "attr1", _ => new(""))
            .AddElementAttribute(kindBType, "attr1", _ => new(""))
            .AddElementAttribute(kindAType, "attr2", _ => new(""))
            .AddElementAttribute(kindBType, "attr2", _ => new(""))
            .AddHierarchyLinks(kindContainsType, kindAType, kindABType, _ => new([]))
            .AddLinks(kindPointsToType, kindAType, kindAType, _ => new([]))
            .AddLinks(kindPointsToType, kindAType, kindBType, _ => new([]))
            .AddLinks(kindPointsToColorBlueType, kindAType, kindBType, _ => new([]))
            .BuildLazy()
            .Schema;

        // Assert

        schema.ElementTypes.Should().BeEquivalentTo(
        [
            anyType, kindAType, kindAColorBlueType, kindBType, kindABType
        ]);

        schema.LinkTypes.Keys.Should().BeEquivalentTo(
        [
            kindContainsType, kindPointsToType, kindPointsToColorBlueType
        ]);
        schema.LinkTypes[kindContainsType].Should().BeEquivalentTo<LinkElementTypes>(
        [
            new(kindAType, kindABType)
        ]);
        schema.LinkTypes[kindPointsToType].Should().BeEquivalentTo<LinkElementTypes>(
        [
            new(kindAType, kindAType),
            new(kindAType, kindBType)
        ]);
        schema.LinkTypes[kindPointsToColorBlueType].Should().BeEquivalentTo<LinkElementTypes>(
        [
            new(kindAType, kindBType)
        ]);

        schema.ElementAttributes.Keys.Should().BeEquivalentTo(
        [
            kindAColorBlueType,
            kindBType,
            kindAType
        ]);
        schema.ElementAttributes[kindAColorBlueType].Should().BeEquivalentTo(["attr1"]);
        schema.ElementAttributes[kindBType].Should().BeEquivalentTo(["attr1", "attr2"]);
        schema.ElementAttributes[kindAType].Should().BeEquivalentTo(["attr2"]);

        schema.RootElementTypes.Should().BeEquivalentTo<ElementType>(
        [
            anyType, kindAType
        ]);

        schema.HierarchyLinkType.Should().Be(kindContainsType);
    }
}
