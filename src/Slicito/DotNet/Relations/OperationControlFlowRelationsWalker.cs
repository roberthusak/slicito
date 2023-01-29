using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

internal class OperationControlFlowRelationsWalker : OperationWalker
{
    private readonly DotNetContext _context;
    private readonly ControlFlowRelations.Builder _builder;

    private DotNetElement _firstElement;
    private DotNetElement _lastElement;

    private OperationControlFlowRelationsWalker(DotNetContext context, DotNetBlock blockElement, ControlFlowRelations.Builder builder)
    {
        _context = context;
        _builder = builder;

        _firstElement = blockElement;
        _lastElement = blockElement;
    }

    public static void VisitMethod(DotNetContext context, DotNetMethod methodElement, ControlFlowRelations.Builder builder)
    {
        if (methodElement.ControlFlowGraph is null)
        {
            return;
        }

        var blocks = methodElement.ControlFlowGraph.Blocks;

        // Connect the method definition with its start (simplifies slicing from methods)
        var entryBlockElement = context.TryGetElementFromBlock(blocks[0]);
        if (entryBlockElement is not null)
        {
            builder.IsSucceededByUnconditionally.Add(methodElement, entryBlockElement, null);
        }

        foreach (var block in blocks)
        {
            // Connect operation elements within blocks

            var blockElement = context.TryGetElementFromBlock(block);
            if (blockElement is null)
            {
                continue;
            }

            var walker = new OperationControlFlowRelationsWalker(context, blockElement, builder);

            foreach (var operation in block.Operations)
            {
                walker.Visit(operation);
            }

            walker.Visit(block.BranchValue);

            // Connect the flow between blocks

            var lastElement = walker._lastElement;

            var firstConditionalElement = context.TryGetElementFromBlock(block.ConditionalSuccessor?.Destination);
            var firstFallthroughElement = context.TryGetElementFromBlock(block.FallThroughSuccessor?.Destination);

            switch (block.ConditionKind)
            {
                case ControlFlowConditionKind.None:
                    AddPairIfTargetNotNull(builder.IsSucceededByUnconditionally, lastElement, firstFallthroughElement, default);
                    break;

                case ControlFlowConditionKind.WhenFalse:
                    AddPairIfTargetNotNull(builder.IsSucceededByIfFalse, lastElement, firstConditionalElement, default);
                    AddPairIfTargetNotNull(builder.IsSucceededByIfTrue, lastElement, firstFallthroughElement, default);
                    break;

                case ControlFlowConditionKind.WhenTrue:
                    AddPairIfTargetNotNull(builder.IsSucceededByIfTrue, lastElement, firstConditionalElement, default);
                    AddPairIfTargetNotNull(builder.IsSucceededByIfFalse, lastElement, firstFallthroughElement, default);
                    break;
            }
        }

        void AddPairIfTargetNotNull(
            Relation<DotNetElement, DotNetElement, SyntaxNode?>.Builder relation,
            DotNetElement source,
            DotNetElement? target,
            SyntaxNode? data)
        {
            if (target is not null)
            {
                relation.Add(source, target, data);
            }
        }
    }

    public override void DefaultVisit(IOperation operation)
    {
        base.DefaultVisit(operation);

        ConnectFlowWithPreviousElement(operation, _builder.IsSucceededByUnconditionally);
    }

    public override void VisitInvocation(IInvocationOperation operation)
    {
        base.DefaultVisit(operation);

        ConnectFlowWithPreviousElement(operation, _builder.IsSucceededByWithLeftOutInvocation);
    }

    public override void VisitObjectCreation(IObjectCreationOperation operation)
    {
        base.DefaultVisit(operation);

        ConnectFlowWithPreviousElement(operation, _builder.IsSucceededByWithLeftOutInvocation);
    }

    private void ConnectFlowWithPreviousElement(IOperation operation, Relation<DotNetElement, DotNetElement, SyntaxNode?>.Builder relation)
    {
        if (_context.TryGetElementFromOperation(operation) is not DotNetOperation operationElement)
        {
            return;
        }

        if (_firstElement == null)
        {
            _firstElement = operationElement;
        }

        if (_lastElement != null)
        {
            relation.Add(_lastElement, operationElement, default);
        }

        _lastElement = operationElement;
    }
}
