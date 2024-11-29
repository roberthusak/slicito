using FluentAssertions;

using Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;
using Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;

namespace Slicito.ProgramAnalysis.Tests;

[TestClass]
public class SmtLibCliSolverTest
{
    private ValueTask<ISolver> CreateSolver() => new SmtLibCliSolverFactory("z3", ["-in"]).CreateSolverAsync();

    [TestMethod]
    public async Task Unsatisfiable_Result_Is_Correct()
    {
        // Arrange
        var solver = await CreateSolver();

        // Act

        await solver.AssertAsync(
            new Term.FunctionApplication(
                Functions.Equals(Sorts.Bool),
                [
                    new Term.Constant.Bool(true),
                    new Term.Constant.Bool(false)
                ]));

        var result = await solver.CheckSatAsync();

        // Assert
        result.Should().Be(SolverResult.Unsat);
    }

    [TestMethod]
    public async Task Satisfiable_Result_And_Model_Are_Correct()
    {
        // Arrange
        var solver = await CreateSolver();

        // Act

        var x = new Function.Nullary("x", Sorts.Bool);
        
        await solver.AssertAsync(
            new Term.FunctionApplication(
                Functions.Equals(Sorts.Bool),
                [
                    new Term.FunctionApplication(x, []),
                    new Term.Constant.Bool(true)
                ])
        );

        Term? xValue = null;

        var result = await solver.CheckSatAsync(model =>
        {
            xValue = model.Evaluate(new Term.FunctionApplication(x, []));
            
            return ValueTask.CompletedTask;
        });

        // Assert
        result.Should().Be(SolverResult.Sat);
        xValue.Should().Be(new Term.Constant.Bool(true));
    }
}
