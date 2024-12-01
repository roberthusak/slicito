using FluentAssertions;

using Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;
using Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;
using Slicito.ProgramAnalysis.Tests.Helpers;

namespace Slicito.ProgramAnalysis.Tests;

[TestClass]
public class SmtLibCliSolverTest
{
    public TestContext? TestContext { get; set; }

    [TestMethod]
    public async Task Unsatisfiable_Condition_Yields_Unsatisfiable_Result()
    {
        // Arrange
        var solver = await SolverHelper.CreateSolverFactory(TestContext!).CreateSolverAsync();

        // Act

        await solver.AssertAsync(Terms.Equal(Terms.True, Terms.False));

        var result = await solver.CheckSatAsync();

        // Assert
        result.Should().Be(SolverResult.Unsat);
    }

    [TestMethod]
    public async Task Satisfiable_Condition_Yields_Satisfiable_Result_And_Correct_Model()
    {
        // Arrange
        var solver = await SolverHelper.CreateSolverFactory(TestContext!).CreateSolverAsync();

        // Act

        var x = new Function.Nullary("x", Sorts.Bool);
        
        await solver.AssertAsync(Terms.Equal(Terms.Constant(x), Terms.True));

        Term? xValue = null;

        var result = await solver.CheckSatAsync(model =>
        {
            xValue = model.Evaluate(Terms.Constant(x));
            
            return ValueTask.CompletedTask;
        });

        // Assert
        result.Should().Be(SolverResult.Sat);
        xValue.Should().Be(Terms.True);
    }

    [TestMethod]
    public async Task DeMorgans_Law_On_BitVec64_Is_Valid()
    {
        // Arrange
        var solver = await SolverHelper.CreateSolverFactory(TestContext!).CreateSolverAsync();

        // Act

        var x = new Function.Nullary("x", Sorts.BitVec64);
        var y = new Function.Nullary("y", Sorts.BitVec64);

        await solver.AssertAsync(
            Terms.Not(
                Terms.Equal(
                    Terms.BitVec.BitwiseAnd(
                        Terms.BitVec.BitwiseNot(Terms.Constant(x)),
                        Terms.BitVec.BitwiseNot(Terms.Constant(y))),
                    Terms.BitVec.BitwiseNot(
                        Terms.BitVec.BitwiseOr(Terms.Constant(x), Terms.Constant(y))))));

        var result = await solver.CheckSatAsync();

        // Assert
        result.Should().Be(SolverResult.Unsat);
    }

    [TestMethod]
    public async Task Unsigned_BitVec64_Add_Yields_Correct_Model()
    {
        // Arrange
        var solver = await SolverHelper.CreateSolverFactory(TestContext!).CreateSolverAsync();

        // Act

        var x = new Function.Nullary("x", Sorts.BitVec64);

        await solver.AssertAsync(
            Terms.Equal(
                Terms.BitVec.Add(
                    Terms.BitVec.Literal(37, Sorts.BitVec64),
                    Terms.BitVec.Literal(5, Sorts.BitVec64)),
                Terms.Constant(x)));

        Term? xValue = null;
        var result = await solver.CheckSatAsync(model =>
        {
            xValue = model.Evaluate(Terms.Constant(x));
            
            return ValueTask.CompletedTask;
        });

        // Assert
        result.Should().Be(SolverResult.Sat);
        xValue.Should().Be(Terms.BitVec.Literal(42, Sorts.BitVec64));
    }
}
