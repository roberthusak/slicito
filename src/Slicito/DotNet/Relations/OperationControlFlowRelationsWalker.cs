using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

internal class OperationControlFlowRelationsWalker : OperationWalker
{
    private readonly DotNetContext _context;
    private readonly ControlFlowRelations.Builder _builder;

    private DotNetOperation? _firstElement;
    private DotNetOperation? _lastElement;

    private OperationControlFlowRelationsWalker(DotNetContext context, ControlFlowRelations.Builder builder)
    {
        _context = context;
        _builder = builder;
    }

    public static void VisitMethod(DotNetContext context, DotNetMethod method, ControlFlowRelations.Builder builder)
    {
        if (method.ControlFlowGraph is null)
        {
            return;
        }

        var blocks = method.ControlFlowGraph.Blocks;

        var firstAndLastElements = new (DotNetOperation? first, DotNetOperation? last)[blocks.Length];

        // Connect operation elements within blocks, find the first and the last element of each block
        foreach (var block in blocks)
        {
            var walker = new OperationControlFlowRelationsWalker(context, builder);

            foreach (var operation in block.Operations)
            {
                walker.Visit(operation);
            }

            walker.Visit(block.BranchValue);

            Debug.Assert(walker._firstElement is not null || block.ConditionalSuccessor is null);

            firstAndLastElements[block.Ordinal] = (walker._firstElement, walker._lastElement);
        }

        // Connect the flow between blocks
        foreach (var block in blocks)
        {
            var lastElement = firstAndLastElements[block.Ordinal].last;
            if (lastElement is null)
            {
                continue;
            }

            var firstConditionalElement = TryGetFirstElement(block.ConditionalSuccessor?.Destination);
            var firstFallthroughElement = TryGetFirstElement(block.FallThroughSuccessor?.Destination);

            switch (block.ConditionKind)
            {
                case ControlFlowConditionKind.None:
                    AddPairIfTargetNotNull(builder.IsSucceededByUnconditionally, lastElement, firstFallthroughElement, default);
                    break;

                case ControlFlowConditionKind.WhenFalse:
                    AddPairIfTargetNotNull(builder.IsSucceededByIfFalse, lastElement, firstConditionalElement, lastElement.Operation.Syntax);
                    AddPairIfTargetNotNull(builder.IsSucceededByIfTrue, lastElement, firstFallthroughElement, lastElement.Operation.Syntax);
                    break;

                case ControlFlowConditionKind.WhenTrue:
                    AddPairIfTargetNotNull(builder.IsSucceededByIfTrue, lastElement, firstConditionalElement, lastElement.Operation.Syntax);
                    AddPairIfTargetNotNull(builder.IsSucceededByIfFalse, lastElement, firstFallthroughElement, lastElement.Operation.Syntax);
                    break;
            }
        }

        DotNetOperation? TryGetFirstElement(BasicBlock? block)
        {
            while (block is not null && firstAndLastElements[block.Ordinal].first is null)
            {
                Debug.Assert(block.ConditionalSuccessor is null);

                block = block.FallThroughSuccessor?.Destination;
            }

            return block is null ? null : firstAndLastElements[block.Ordinal].first;
        }

        void AddPairIfTargetNotNull(
            BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?>.Builder relation,
            DotNetOperation source,
            DotNetOperation? target,
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

    private void ConnectFlowWithPreviousElement(IOperation operation, BinaryRelation<DotNetOperation, DotNetOperation, SyntaxNode?>.Builder relation)
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
