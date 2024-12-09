using System.Collections.Immutable;

using Slicito.Abstractions.Models;
using Slicito.ProgramAnalysis.Notation;

namespace Controllers;

public static class FlowGraphHelper
{
    public record AdditionalEdge(BasicBlock Source, BasicBlock Target, string? Label = null, Command? Command = null);

    public static IFlowGraph CreateSampleFlowGraph()
    {
        // Create variables with 32-bit signed integer type
        var intType = new DataType.Integer(true, 32);
        var varA = new Variable("a", intType);
        var varB = new Variable("b", intType);

        var builder = new FlowGraph.Builder([varA, varB], [new Expression.VariableReference(varB)]);

        // Create blocks for each statement/condition
        var condition1 = new BasicBlock.Inner(new Operation.ConditionalJump(
            new Expression.BinaryOperator(
                BinaryOperatorKind.Equal,
                new Expression.VariableReference(varB),
                new Expression.Constant.SignedInteger(0, new DataType.Integer(false, 32)))));

        var trueBranch1 = new BasicBlock.Inner(new Operation.Assignment(
            new Location.VariableReference(varB),
            new Expression.BinaryOperator(
                BinaryOperatorKind.Add,
                new Expression.VariableReference(varB),
                new Expression.Constant.SignedInteger(2, new DataType.Integer(false, 32)))));

        var condition2 = new BasicBlock.Inner(new Operation.ConditionalJump(
            new Expression.BinaryOperator(
                BinaryOperatorKind.GreaterThan,
                new Expression.VariableReference(varA),
                new Expression.Constant.SignedInteger(8, new DataType.Integer(false, 32)))));

        var trueBranch2 = new BasicBlock.Inner(new Operation.Assignment(
            new Location.VariableReference(varB),
            new Expression.BinaryOperator(
                BinaryOperatorKind.Subtract,
                new Expression.BinaryOperator(
                    BinaryOperatorKind.Subtract,
                    new Expression.VariableReference(varB),
                    new Expression.VariableReference(varA)),
                new Expression.Constant.SignedInteger(1, new DataType.Integer(false, 32)))));

        var elseBranch = new BasicBlock.Inner(new Operation.Assignment(
            new Location.VariableReference(varB),
            new Expression.BinaryOperator(
                BinaryOperatorKind.Multiply,
                new Expression.VariableReference(varB),
                new Expression.Constant.SignedInteger(2, new DataType.Integer(false, 32)))));

        var assign3 = new BasicBlock.Inner(new Operation.Assignment(
            new Location.VariableReference(varB),
            new Expression.VariableReference(varB)));

        // Add blocks to the builder
        builder.AddBlock(condition1);
        builder.AddBlock(trueBranch1);
        builder.AddBlock(condition2);
        builder.AddBlock(trueBranch2);
        builder.AddBlock(elseBranch);
        builder.AddBlock(assign3);

        // Connect blocks with edges
        builder.AddUnconditionalEdge(builder.Entry, condition1);
        builder.AddTrueEdge(condition1, trueBranch1);
        builder.AddFalseEdge(condition1, condition2);
        builder.AddUnconditionalEdge(trueBranch1, assign3);
        builder.AddTrueEdge(condition2, trueBranch2);
        builder.AddFalseEdge(condition2, elseBranch);
        builder.AddUnconditionalEdge(trueBranch2, elseBranch);
        builder.AddUnconditionalEdge(elseBranch, assign3);
        builder.AddUnconditionalEdge(assign3, builder.Exit);

        return builder.Build();
    }

    public static Graph CreateGraphModel(IFlowGraph flowGraph, IEnumerable<AdditionalEdge>? additionalEdges = null)
    {
        var nodeIdMap = flowGraph.Blocks
            .Select((b, i) => new { b, i })
            .ToDictionary(x => x.b, x => x.i.ToString());

        var nodes = flowGraph.Blocks.Select(b => new Node(
            nodeIdMap[b],
            BlockLabelProvider.GetLabel(b),
            null
        )).ToImmutableArray();

        var edges = new List<Edge>();
        foreach (var block in flowGraph.Blocks)
        {
            var trueSuccessor = flowGraph.GetTrueSuccessor(block);
            var falseSuccessor = flowGraph.GetFalseSuccessor(block);
            var unconditionalSuccessor = flowGraph.GetUnconditionalSuccessor(block);

            if (trueSuccessor != null)
            {
                edges.Add(new Edge(nodeIdMap[block], nodeIdMap[trueSuccessor], "true"));
            }
            if (falseSuccessor != null)
            {
                edges.Add(new Edge(nodeIdMap[block], nodeIdMap[falseSuccessor], "false"));
            }
            if (unconditionalSuccessor != null)
            {
                edges.Add(new Edge(nodeIdMap[block], nodeIdMap[unconditionalSuccessor], null));
            }
        }

        foreach (var edge in additionalEdges ?? [])
        {
            edges.Add(new Edge(
                nodeIdMap[edge.Source],
                nodeIdMap[edge.Target],
                edge.Label,
                edge.Command));
        }

        return new Graph(nodes, edges.ToImmutableArray());
    }
}
