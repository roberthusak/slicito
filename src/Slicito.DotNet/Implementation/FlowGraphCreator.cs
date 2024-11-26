using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

using Slicito.ProgramAnalysis.Notation;

using SlicitoBasicBlock = Slicito.ProgramAnalysis.Notation.BasicBlock;
using SlicitoLocation = Slicito.ProgramAnalysis.Notation.Location;

namespace Slicito.DotNet.Implementation;

internal class FlowGraphCreator
{
    public static IFlowGraph? TryCreateFlowGraph(IMethodSymbol method, Solution solution)
    {
        // TODO: Implement actual flow graph creation

        var builder = new FlowGraph.Builder();

        // Create variables with 32-bit signed integer type
        var intType = new DataType.Integer(true, 32);
        var varA = new Variable("a", intType);

        var block = new SlicitoBasicBlock.Inner(new Operation.Assignment(
            new SlicitoLocation.VariableReference(varA),
            new Expression.Constant.SignedInteger(42, intType)));

        builder.AddBlock(block);

        builder.AddUnconditionalEdge(builder.Entry, block);
        builder.AddUnconditionalEdge(block, builder.Exit);

        return builder.Build();
    }
}
