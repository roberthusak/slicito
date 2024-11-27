using Controllers;

using FluentAssertions;

using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Slicito.Abstractions;
using Slicito.ProgramAnalysis;
using Slicito.ProgramAnalysis.Notation;
using Slicito.Queries;

namespace Slicito.DotNet.Tests;

[TestClass]
public class DotNetExtractorTests
{
    private static DotNetSolutionContext? _solutionContext;
    private static IEnumerable<(ElementInfo Method, string DisplayName)>? _methods;
    
    [ClassInitialize]
    public static async Task Initialize(TestContext _)
    {
        const string solutionPath = @"..\..\..\..\inputs\AnalysisSamples\AnalysisSamples.sln";
        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(solutionPath);
        
        var types = new DotNetTypes(new TypeSystem());
        var sliceManager = new SliceManager();
        
        _solutionContext = new DotNetSolutionContext(solution, types, sliceManager);
        
        _methods = await DotNetMethodHelper.GetAllMethodsWithDisplayNamesAsync(
            _solutionContext.LazySlice, 
            types);
    }

    [TestMethod]
    [DynamicData(nameof(GetMethodTestData), DynamicDataSourceType.Method)]
    public void CanCreateFlowGraph_ForMethod(ElementInfo method, string displayName)
    {
        // Arrange
        _solutionContext.Should().NotBeNull("Solution context should be initialized");

        // Act
        var flowGraph = _solutionContext!.TryGetFlowGraph(method.Id);

        // Assert
        flowGraph.Should().NotBeNull($"Failed to create flow graph for method {displayName}");
    }

    private static IEnumerable<object[]> GetMethodTestData()
    {
        _methods.Should().NotBeNull("Methods should be initialized");
        
        return _methods!.Select(m => new object[] { m.Method, m.DisplayName });
    }
}