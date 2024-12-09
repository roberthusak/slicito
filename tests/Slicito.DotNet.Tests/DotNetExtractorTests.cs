using FluentAssertions;

using Microsoft.CodeAnalysis.MSBuild;

using Slicito.Abstractions;
using Slicito.Common;
using Slicito.ProgramAnalysis.Interprocedural;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.DotNet.Tests;

[TestClass]
public class DotNetExtractorTests
{
    private static DotNetSolutionContext? _solutionContext;
    private static IEnumerable<(ElementInfo Method, string DisplayName)>? _methods;
    private static DotNetTypes? _dotNetTypes;
    [ClassInitialize]
    public static async Task Initialize(TestContext _)
    {
        const string solutionPath = @"..\..\..\..\inputs\AnalysisSamples\AnalysisSamples.sln";
        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(solutionPath);
        
        _dotNetTypes = new DotNetTypes(new TypeSystem());
        var sliceManager = new SliceManager();
        
        _solutionContext = new DotNetSolutionContext(solution, _dotNetTypes, sliceManager);
        
        _methods = await DotNetMethodHelper.GetAllMethodsWithDisplayNamesAsync(
            _solutionContext.LazySlice, 
            _dotNetTypes);
    }

    [TestMethod]
    [DynamicData(nameof(GetMethodTestData), DynamicDataSourceType.Method)]
    public void Can_Create_Flow_Graph_For_Method(ElementInfo method, string displayName)
    {
        // Arrange
        _solutionContext.Should().NotBeNull("Solution context should be initialized");

        // Act
        var flowGraph = _solutionContext!.TryGetFlowGraph(method.Id);

        // Assert
        flowGraph.Should().NotBeNull($"Failed to create flow graph for method {displayName}");
    }

    [TestMethod]
    [DynamicData(nameof(GetMethodTestData), DynamicDataSourceType.Method)]
    public async Task Can_Create_Call_Graph_For_Method(ElementInfo method, string displayName)
    {
        // Arrange
        _solutionContext.Should().NotBeNull("Solution context should be initialized");

        // Act
        var callGraph = await new CallGraph.Builder(_solutionContext!.LazySlice, _dotNetTypes!)
            .AddCallerRoot(method.Id)
            .BuildAsync();

        // Assert
        callGraph.RootProcedures.Should().ContainSingle(p => p.ProcedureElement.Id == method.Id);
        callGraph.AllProcedures.Should().Contain(p => p.ProcedureElement.Id == method.Id);
    }

    private static IEnumerable<object[]> GetMethodTestData()
    {
        _methods.Should().NotBeNull("Methods should be initialized");
        
        return _methods!.Select(m => new object[] { m.Method, m.DisplayName });
    }

    [TestMethod]
    public void Procedure_Signature_Of_BasicSymbolicExecutionSample_Is_Correct()
    {
        // Arrange
        
        _solutionContext.Should().NotBeNull("Solution context should be initialized");
        _methods.Should().NotBeNull("Methods should be initialized");

        var methodId = _methods!.First(m => m.DisplayName == "AnalysisSamples.Samples.BasicSymbolicExecutionSample").Method.Id;

        // Act
        var signature = _solutionContext!.GetProcedureSignature(methodId);

        // Assert

        signature.Name.Should().Be("AnalysisSamples.AnalysisSamples.Samples.BasicSymbolicExecutionSample(int, int)");

        signature.ParameterTypes.Should().BeEquivalentTo([
            new DataType.Integer(Signed: true, 32),
            new DataType.Integer(Signed: true, 32)
        ]);

        signature.ReturnTypes.Should().BeEquivalentTo([
            new DataType.Integer(Signed: true, 32)
        ]);
    }
}
