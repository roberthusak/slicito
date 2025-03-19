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

        var result = await solver.CheckSatisfiabilityAsync();

        // Assert
        result.Should().Be(SolverResult.Unsatisfiable);
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

        var result = await solver.CheckSatisfiabilityAsync(async model =>
        {
            xValue = await model.EvaluateAsync(Terms.Constant(x));
        });

        // Assert
        result.Should().Be(SolverResult.Satisfiable);
        xValue.Should().Be(Terms.True);
    }

    [TestMethod]
    public async Task DeMorgans_Law_On_BitVec64_Are_Validated()
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

        var result = await solver.CheckSatisfiabilityAsync();

        // Assert
        result.Should().Be(SolverResult.Unsatisfiable);
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
        var result = await solver.CheckSatisfiabilityAsync(async model =>
        {
            xValue = await model.EvaluateAsync(Terms.Constant(x));
        });

        // Assert
        result.Should().Be(SolverResult.Satisfiable);
        xValue.Should().Be(Terms.BitVec.Literal(42, Sorts.BitVec64));
    }

    [TestMethod]
    public async Task Int_To_BitVec_And_Back_To_Natural_Yields_Unsigned_Original_Value()
    {
        // Arrange
        var solver = await SolverHelper.CreateSolverFactory(TestContext!).CreateSolverAsync();

        // Act

        var x = new Function.Nullary("x", Sorts.Int);
        var y = new Function.Nullary("y", Sorts.BitVec8);
        var z = new Function.Nullary("z", Sorts.Int);

        await solver.AssertAsync(
            Terms.Equal(
                Terms.Constant(x),
                Terms.Int.Literal(-42)));

        await solver.AssertAsync(
            Terms.Equal(
                Terms.Constant(y),
                Terms.Int.ToBitVec(
                    Terms.Constant(x),
                    8)));

        await solver.AssertAsync(
            Terms.Equal(
                Terms.Constant(z),
                Terms.BitVec.ToNatural(
                    Terms.Constant(y))));

        Term? xValue = null;
        Term? yValue = null;
        Term? zValue = null;
        var result = await solver.CheckSatisfiabilityAsync(async model =>
        {
            xValue = await model.EvaluateAsync(Terms.Constant(x));
            yValue = await model.EvaluateAsync(Terms.Constant(y));
            zValue = await model.EvaluateAsync(Terms.Constant(z));
        });

        // Assert
        result.Should().Be(SolverResult.Satisfiable);
        xValue.Should().Be(Terms.Int.Literal(-42));
        yValue.Should().Be(Terms.BitVec.Literal(unchecked((byte) -42), Sorts.BitVec8));
        zValue.Should().Be(Terms.Int.Literal(unchecked((byte) -42)));
    }

    [TestMethod]
    public async Task Unicode_Strings_With_Special_Characters_Are_Correctly_Serialized_And_Parsed()
    {
        // Arrange
        var solver = await SolverHelper.CreateSolverFactory(TestContext!).CreateSolverAsync();

        // Act

        var testString = "Příliš žluťoučký kůň\núpěl ďábelské ódy\t\u27E8";

        var x = new Function.Nullary("x", Sorts.String);

        await solver.AssertAsync(
            Terms.Equal(
                Terms.Constant(x),
                Terms.String.Literal(testString)));

        Term? xValue = null;
        var result = await solver.CheckSatisfiabilityAsync(async model =>
        {
            xValue = await model.EvaluateAsync(Terms.Constant(x));
        });

        // Assert
        result.Should().Be(SolverResult.Satisfiable);
        xValue.Should().Be(Terms.String.Literal(testString));
    }

    [TestMethod]
    public async Task Produces_Correct_String_For_Simple_Regular_Expression()
    {
        // Arrange
        var solver = await SolverHelper.CreateSolverFactory(TestContext!).CreateSolverAsync();

        // Act

        var x = new Function.Nullary("x", Sorts.String);

        await solver.AssertAsync(
            Terms.String.IsInRegLan(
                Terms.Constant(x),
                Terms.RegLan.Loop(
                    Terms.String.ToRegLan(
                        Terms.String.Literal("ab")),
                    1,
                    3)));

        await solver.AssertAsync(
            Terms.Equal(
                Terms.String.Length(
                    Terms.Constant(x)),
                Terms.Int.Literal(6)));

        Term? xValue = null;
        var result = await solver.CheckSatisfiabilityAsync(async model =>
        {
            xValue = await model.EvaluateAsync(Terms.Constant(x));
        });

        // Assert
        result.Should().Be(SolverResult.Satisfiable);
        xValue.Should().Be(Terms.String.Literal("ababab"));
    }
}
