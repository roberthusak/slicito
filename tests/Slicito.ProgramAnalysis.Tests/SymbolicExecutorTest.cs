using FluentAssertions;

using Microsoft.CodeAnalysis.MSBuild;

using Slicito.Common;
using Slicito.DotNet;
using Slicito.ProgramAnalysis.Notation;
using Slicito.ProgramAnalysis.SymbolicExecution;
using Slicito.ProgramAnalysis.Tests.Helpers;

namespace Slicito.ProgramAnalysis.Tests;

[TestClass]
public class SymbolicExecutorTest
{
    private static DotNetTypes? _types;
    private static DotNetSolutionContext? _solutionContext;

    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static async Task Initialize(TestContext _)
    {
        const string solutionPath = @"..\..\..\..\inputs\AnalysisSamples\AnalysisSamples.sln";
        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(solutionPath);
        
        var typeSystem = new TypeSystem();
        _types = new DotNetTypes(typeSystem);
        var sliceManager = new SliceManager(typeSystem);
        
        _solutionContext = new DotNetSolutionContext(solution, _types, sliceManager);
    }

    [TestMethod]
    public async Task Finds_Counterexample_For_Assertion_In_BasicSymbolicExecutionSample()
    {
        // Arrange

        _solutionContext.Should().NotBeNull("Solution context should be initialized");
        _types.Should().NotBeNull(".NET link and element types should be initialized");

        var methods = await DotNetMethodHelper.GetAllMethodsWithDisplayNamesAsync(_solutionContext!.Slice, _types!);
        var method = methods.Single(m => m.DisplayName == "AnalysisSamples.Samples.BasicSymbolicExecutionSample").Method;
        
        var flowGraph = _solutionContext.TryGetFlowGraph(method.Id);

        flowGraph.Should().NotBeNull("Flow graph should be created for the method");

        var targetBlock = flowGraph!.Blocks
            .OfType<BasicBlock.Inner>()
            .Single(b =>
                b.Operation is Operation.Assignment
                {
                    Location: Location.VariableReference { Variable.Name: "b" },
                    Value: Expression.VariableReference { Variable.Name: "b" }
                });

        var symbolicExecutor = new SymbolicExecutor(SolverHelper.CreateSolverFactory(TestContext!));

        // Act

        var result = await symbolicExecutor.ExecuteAsync(flowGraph, [targetBlock]);

        var executionModel = (result as ExecutionResult.Reachable)?.ExecutionModel;

        var aIntValue = executionModel?.ParameterValues[0] as Expression.Constant.SignedInteger;
        var bIntValue = executionModel?.ParameterValues[1] as Expression.Constant.SignedInteger;

        // Assert

        result.Should().BeOfType<ExecutionResult.Reachable>();
        
        executionModel.Should().NotBeNull();
        executionModel!.ParameterValues.Should().HaveCount(2);

        aIntValue.Should().NotBeNull();
        bIntValue.Should().NotBeNull();

        aIntValue!.Value.Should().BeGreaterThan(8);
        ((int)((bIntValue!.Value - aIntValue.Value - 1) * 2)).Should().Be(0);
    }
}
