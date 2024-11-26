using System.Collections.Immutable;
using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.ProgramAnalysis.Notation;

namespace Controllers;

public class SampleFlowGraphBrowser : IController
{
    private readonly IFlowGraph _flowGraph;

    public SampleFlowGraphBrowser(IFlowGraph? flowGraph = null)
    {
        _flowGraph = flowGraph ?? CreateSampleFlowGraph();
    }

    private static IFlowGraph CreateSampleFlowGraph()
    {
        var builder = new FlowGraph.Builder();

        // Create variables with 32-bit signed integer type
        var intType = new DataType.Integer(true, 32);
        var varA = new Variable("a", intType);
        var varB = new Variable("b", intType);

        // Create blocks for each statement/condition
        var assign1 = new BasicBlock.Inner(new Operation.Assignment(
            new Location.VariableReference(varA),
            new Expression.VariableReference(varA)));

        var assign2 = new BasicBlock.Inner(new Operation.Assignment(
            new Location.VariableReference(varB),
            new Expression.VariableReference(varB)));

        var condition1 = new BasicBlock.Inner(new Operation.ConditionalJump(
            new Expression.BinaryOperator(
                new Expression.VariableReference(varB),
                new Expression.Constant.SignedInteger(0, new DataType.Integer(false, 32)),
                BinaryOperatorKind.Equal)));

        var trueBranch1 = new BasicBlock.Inner(new Operation.Assignment(
            new Location.VariableReference(varB),
            new Expression.BinaryOperator(
                new Expression.VariableReference(varB),
                new Expression.Constant.SignedInteger(2, new DataType.Integer(false, 32)),
                BinaryOperatorKind.Add)));

        var condition2 = new BasicBlock.Inner(new Operation.ConditionalJump(
            new Expression.BinaryOperator(
                new Expression.VariableReference(varA),
                new Expression.Constant.SignedInteger(8, new DataType.Integer(false, 32)),
                BinaryOperatorKind.GreaterThan)));

        var trueBranch2 = new BasicBlock.Inner(new Operation.Assignment(
            new Location.VariableReference(varB),
            new Expression.BinaryOperator(
                new Expression.BinaryOperator(
                    new Expression.VariableReference(varB),
                    new Expression.VariableReference(varA),
                    BinaryOperatorKind.Subtract),
                new Expression.Constant.SignedInteger(1, new DataType.Integer(false, 32)),
                BinaryOperatorKind.Subtract)));

        var elseBranch = new BasicBlock.Inner(new Operation.Assignment(
            new Location.VariableReference(varB),
            new Expression.BinaryOperator(
                new Expression.VariableReference(varB),
                new Expression.Constant.SignedInteger(2, new DataType.Integer(false, 32)),
                BinaryOperatorKind.Multiply)));

        var assign3 = new BasicBlock.Inner(new Operation.Assignment(
            new Location.VariableReference(varB),
            new Expression.VariableReference(varB)));

        // Add blocks to the builder
        builder.AddBlock(assign1);
        builder.AddBlock(assign2);
        builder.AddBlock(condition1);
        builder.AddBlock(trueBranch1);
        builder.AddBlock(condition2);
        builder.AddBlock(trueBranch2);
        builder.AddBlock(elseBranch);
        builder.AddBlock(assign3);

        // Connect blocks with edges
        builder.AddUnconditionalEdge(builder.Entry, assign1);
        builder.AddUnconditionalEdge(assign1, assign2);
        builder.AddUnconditionalEdge(assign2, condition1);
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

    public Task<IModel> InitAsync()
    {
        return Task.FromResult<IModel>(CreateGraphModel());
    }

    public Task<IModel?> ProcessCommandAsync(Command command)
    {
        // For now, we don't handle any commands as this is a static visualization
        return Task.FromResult<IModel?>(null);
    }

    private Graph CreateGraphModel()
    {
        var nodeIdMap = _flowGraph.Blocks
            .Select((b, i) => new { b, i })
            .ToDictionary(x => x.b, x => x.i.ToString());

        var nodes = _flowGraph.Blocks.Select(b => new Node(
            nodeIdMap[b],
            BlockLabelProvider.GetLabel(b),
            null
        )).ToImmutableArray();

        var edges = new List<Edge>();
        foreach (var block in _flowGraph.Blocks)
        {
            var trueSuccessor = _flowGraph.GetTrueSuccessor(block);
            var falseSuccessor = _flowGraph.GetFalseSuccessor(block);
            var unconditionalSuccessor = _flowGraph.GetUnconditionalSuccessor(block);

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

        return new Graph(nodes, edges.ToImmutableArray());
    }
} 