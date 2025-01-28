using FluentAssertions;

using Microsoft.CodeAnalysis.MSBuild;
using Slicito.Common;
using Slicito.DotNet;
using Slicito.ProgramAnalysis.Notation;
using Slicito.ProgramAnalysis.Reachability;
using Slicito.ProgramAnalysis.Tests.Helpers;

namespace Slicito.ProgramAnalysis.Tests;

[TestClass]
public class ReachabilityAnalysisTest
{
    private static DotNetTypes? _types;
    private static DotNetSolutionContext? _solutionContext;

    public TestContext? TestContext { get; set; }

    [ClassInitialize]
    public static async Task Initialize(TestContext _)
    {
        const string solutionPath = @"..\..\..\..\inputs\AnalysisSamples\AnalysisSamples.sln";
        var solution = await MSBuildWorkspace.Create().OpenSolutionAsync(solutionPath);

        _types = new DotNetTypes(new TypeSystem());
        var sliceManager = new SliceManager();

        _solutionContext = new DotNetSolutionContext(solution, _types, sliceManager);
    }

    [TestMethod]
    public async Task Finds_Input_For_a_Larger_Than_42_For_Which_ConditionalReachabilitySample_Returns_True()
    {
        // Arrange

        _solutionContext.Should().NotBeNull("Solution context should be initialized");
        _types.Should().NotBeNull(".NET link and element types should be initialized");

        var methods = await DotNetMethodHelper.GetAllMethodsWithDisplayNamesAsync(_solutionContext!.LazySlice, _types!);
        var method = methods.Single(m => m.DisplayName == "AnalysisSamples.Samples.ConditionalReachabilitySample").Method;

        // Act

        var reachabilityAnalysis = new ReachabilityAnalysis.Builder(_solutionContext, SolverHelper.CreateSolverFactory(TestContext!))
            .WithProcedureEntryToExit(method, options =>
            {
                var a = options.GetIntegerParameter("a");
                options.AddConstraint(a > 42);

                var returned = options.GetBooleanReturnValue();
                options.AddConstraint(returned);
            })
            .Build();

        var result = await reachabilityAnalysis.AnalyzeAsync();
        var reachableResult = result as ReachabilityResult.Reachable;

        var aIntValue = reachableResult!.Assignments.SingleOrDefault(a => a.Key.Name == "a").Value as Expression.Constant.SignedInteger;
        var bIntValue = reachableResult!.Assignments.SingleOrDefault(a => a.Key.Name == "b").Value as Expression.Constant.SignedInteger;

        // Assert

        result.Should().BeOfType<ReachabilityResult.Reachable>();

        reachableResult!.Assignments.Keys.Select(v => v.Name).Should().BeEquivalentTo(["a", "b"]);

        aIntValue.Should().NotBeNull();
        bIntValue.Should().NotBeNull();

        aIntValue!.Value.Should().BeGreaterThan(42);
        aIntValue!.Value.Should().Be(bIntValue!.Value + 1);
    }
}
