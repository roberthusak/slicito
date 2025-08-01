using System.Collections.Immutable;

using FluentAssertions;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Abstractions.Facts.Attributes;

namespace Slicito.Common.Tests;

[TestClass]
public class TypedSliceFragmentTests
{
    [Kind("Project")]
    public interface IProjectElement : INamedElement
    {
    }

    public interface ITestProjectSliceFragment : ITypedSliceFragment
    {
        ValueTask<IEnumerable<IProjectElement>> GetProjectsAsync();
    }

    public interface ITestProjectSliceFragmentBuilder : ITypedSliceFragmentBuilder<ITestProjectSliceFragment>
    {
        [RootElement(typeof(IProjectElement))]
        ITestProjectSliceFragmentBuilder AddProject(ElementId id, string name);
    }

    public interface IGenericSliceFragmentBuilder<TFragment, TBuilder> : ITypedSliceFragmentBuilder<TFragment>
        where TFragment : ITypedSliceFragment
    {
        [RootElement(typeof(IProjectElement))]
        TBuilder AddProject(ElementId id, string name);
    }

    public interface INestedTestProjectSliceFragmentBuilder : IGenericSliceFragmentBuilder<ITestProjectSliceFragment, INestedTestProjectSliceFragmentBuilder>
    {
    }

    [TestMethod]
    public async Task Built_ITestProjectSliceFragment_Contains_Added_IProjectElements_And_Expected_Slice()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var sliceManager = new SliceManager(typeSystem);

        // Act
        var sliceFragment = await sliceManager.CreateTypedBuilder<ITestProjectSliceFragmentBuilder>()
            .AddProject(new ElementId("ProjectA"), "Project A")
            .AddProject(new ElementId("ProjectB"), "Project B")
            .BuildAsync();

        // Assert

        var projects = await sliceFragment.GetProjectsAsync();
        projects.Should().HaveCount(2);
        projects.Should().Contain(p => p.Id == new ElementId("ProjectA") && p.Name == "Project A");
        projects.Should().Contain(p => p.Id == new ElementId("ProjectB") && p.Name == "Project B");

        var expectedElementType = typeSystem.GetElementTypeFromInterface(typeof(IProjectElement));

        var rootElements = await sliceFragment.Slice.GetRootElementsAsync();
        rootElements.Should().HaveCount(2);
        rootElements.Should().Contain(p => p.Id == new ElementId("ProjectA") && p.Type == expectedElementType);
        rootElements.Should().Contain(p => p.Id == new ElementId("ProjectB") && p.Type == expectedElementType);

        var nameProvider = sliceFragment.Slice.GetElementAttributeProviderAsyncCallback(CommonAttributeNames.Name);
        (await nameProvider(new ElementId("ProjectA"))).Should().Be("Project A");
        (await nameProvider(new ElementId("ProjectB"))).Should().Be("Project B");

        var schema = sliceFragment.Slice.Schema;
        schema.ElementTypes.Should().BeEquivalentTo([expectedElementType]);
        schema.LinkTypes.Should().BeEmpty();
        schema.ElementAttributes.Should().BeEquivalentTo(
            new Dictionary<ElementType, ImmutableArray<string>>
            {
                [expectedElementType] = [CommonAttributeNames.Name],
            });
        schema.RootElementTypes.Should().BeEquivalentTo([expectedElementType]);
        schema.HierarchyLinkType.Should().BeNull();
    }

    [TestMethod]
    public async Task Built_INestedTestProjectSliceFragment_Contains_Added_IProjectElement()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var sliceManager = new SliceManager(typeSystem);

        // Act
        var sliceFragment = await sliceManager.CreateTypedBuilder<INestedTestProjectSliceFragmentBuilder>()
            .AddProject(new ElementId("ProjectA"), "Project A")
            .BuildAsync();

        // Assert
        var projects = await sliceFragment.GetProjectsAsync();
        projects.Should().HaveCount(1);
        projects.Should().Contain(p => p.Id == new ElementId("ProjectA"));
    }
    
    [Kind("Test")]
    public interface ITestElementWithAttributes : INamedElement
    {
        string Foo { get; }
        string Bar { get; }
    }

    public interface IAttributesProjectSliceFragment : ITypedSliceFragment
    {
        ValueTask<IEnumerable<ITestElementWithAttributes>> GetTestElementsAsync();
    }

    public interface IAttributesProjectSliceFragmentBuilder : ITypedSliceFragmentBuilder<IAttributesProjectSliceFragment>
    {
        [RootElement(typeof(ITestElementWithAttributes))]
        IAttributesProjectSliceFragmentBuilder AddTestElementWithWeirdAttributeOrder(ElementId id, string bar, string name, string foo);
    }

    [TestMethod]
    public async Task Built_IAttributesProjectSliceFragment_Contains_Added_ITestElementWithAttributes()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var sliceManager = new SliceManager(typeSystem);

        // Act
        var sliceFragment = await sliceManager.CreateTypedBuilder<IAttributesProjectSliceFragmentBuilder>()
            .AddTestElementWithWeirdAttributeOrder(new ElementId("TestElement"), "Bar Value", "Name Value", "Foo Value")
            .BuildAsync();

        // Assert

        var testElements = await sliceFragment.GetTestElementsAsync();
        testElements.Should().HaveCount(1);

        var element = testElements.Single();
        element.Id.Should().Be(new ElementId("TestElement"));
        element.Name.Should().Be("Name Value");
        element.Foo.Should().Be("Foo Value");
        element.Bar.Should().Be("Bar Value");
    }

    public class InvalidSliceFragmentBuilder1 : ITypedSliceFragmentBuilder<ITestProjectSliceFragment>
    {
        public ValueTask<ITestProjectSliceFragment> BuildAsync() => throw new NotImplementedException();
    }

    public class InvalidSliceFragment1 : ITypedSliceFragment
    {
        public ISlice Slice => throw new NotImplementedException();
    }

    public interface IInvalidSliceFragmentBuilder2 : ITypedSliceFragmentBuilder<InvalidSliceFragment1>
    {
    }

    public interface IInvalidSliceFragmentBuilder3 : ITypedSliceFragmentBuilder<ITestProjectSliceFragment>
    {
        IInvalidSliceFragmentBuilder3 UnannotatedMethod(ElementId id);
    }

    public interface IInvalidSliceFragmentBuilder4 : ITypedSliceFragmentBuilder<ITestProjectSliceFragment>
    {
        [RootElement(typeof(IProjectElement))]
        IInvalidSliceFragmentBuilder4 MethodWithoutParameters();
    }

    public interface IInvalidSliceFragment2 : ITypedSliceFragment
    {
        void MethodWithoutReturnType();
    }

    public interface IInvalidSliceFragmentBuilder5 : ITypedSliceFragmentBuilder<IInvalidSliceFragment2>
    {
    }

    [TestMethod]
    public void Passing_Invalid_Types_To_CreateTypedBuilder_Throws_ArgumentException()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var sliceManager = new SliceManager(typeSystem);

        // Act & Assert
        sliceManager.Invoking(sm => sm.CreateTypedBuilder<InvalidSliceFragmentBuilder1>())
            .Should().Throw<ArgumentException>();
        sliceManager.Invoking(sm => sm.CreateTypedBuilder<IInvalidSliceFragmentBuilder2>())
            .Should().Throw<ArgumentException>();
        sliceManager.Invoking(sm => sm.CreateTypedBuilder<IInvalidSliceFragmentBuilder3>())
            .Should().Throw<ArgumentException>();
        sliceManager.Invoking(sm => sm.CreateTypedBuilder<IInvalidSliceFragmentBuilder4>())
            .Should().Throw<ArgumentException>();
        sliceManager.Invoking(sm => sm.CreateTypedBuilder<IInvalidSliceFragmentBuilder5>())
            .Should().Throw<ArgumentException>();
    }
}
