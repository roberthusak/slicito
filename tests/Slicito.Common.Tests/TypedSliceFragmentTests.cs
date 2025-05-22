using FluentAssertions;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Abstractions.Facts.Attributes;

namespace Slicito.Common.Tests;

[TestClass]
public class TypedSliceFragmentTests
{
    [Kind("Project")]
    public interface IProjectElement : IElement
    {
    }

    public interface ITestProjectSliceFragment : ITypedSliceFragment
    {
        ValueTask<IEnumerable<IProjectElement>> GetProjectsAsync();
    }

    public interface ITestProjectSliceFragmentBuilder : ITypedSliceFragmentBuilder<ITestProjectSliceFragment>
    {
        [RootElement(typeof(IProjectElement))]
        ITestProjectSliceFragmentBuilder AddProject(ElementId id);
    }

    [TestMethod]
    public async Task Built_ITestProjectSliceFragment_Contains_Added_IProjectElements_And_Expected_Slice()
    {
        // Arrange
        var typeSystem = new TypeSystem();
        var sliceManager = new SliceManager(typeSystem);

        // Act
        var sliceFragment = await sliceManager.CreateTypedBuilder<ITestProjectSliceFragmentBuilder>()
            .AddProject(new ElementId("ProjectA"))
            .AddProject(new ElementId("ProjectB"))
            .BuildAsync();

        // Assert

        var projects = await sliceFragment.GetProjectsAsync();
        projects.Should().HaveCount(2);
        projects.Should().Contain(p => p.Id == new ElementId("ProjectA"));
        projects.Should().Contain(p => p.Id == new ElementId("ProjectB"));

        var expectedElementType = typeSystem.GetElementTypeFromInterface(typeof(IProjectElement));

        var rootElements = await sliceFragment.Slice.GetRootElementsAsync();
        rootElements.Should().HaveCount(2);
        rootElements.Should().Contain(p => p.Id == new ElementId("ProjectA") && p.Type == expectedElementType);
        rootElements.Should().Contain(p => p.Id == new ElementId("ProjectB") && p.Type == expectedElementType);

        var schema = sliceFragment.Slice.Schema;
        schema.ElementTypes.Should().BeEquivalentTo([expectedElementType]);
        schema.LinkTypes.Should().BeEmpty();
        schema.ElementAttributes.Should().BeEmpty();
        schema.RootElementTypes.Should().BeEquivalentTo([expectedElementType]);
        schema.HierarchyLinkType.Should().BeNull();
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
