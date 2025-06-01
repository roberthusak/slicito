using System.Collections.Immutable;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;

using Slicito.Abstractions;
using Slicito.ProgramAnalysis.Notation;

using RoslynBasicBlock = Microsoft.CodeAnalysis.FlowAnalysis.BasicBlock;

using SlicitoBasicBlock = Slicito.ProgramAnalysis.Notation.BasicBlock;
using SlicitoLocation = Slicito.ProgramAnalysis.Notation.Location;

namespace Slicito.DotNet.Implementation;

internal class FlowGraphCreator
{
    private readonly ControlFlowGraph _roslynCfg;
    private readonly Project _project;
    private readonly ElementCache _elementCache;

    private readonly Dictionary<RoslynBasicBlock, (SlicitoBasicBlock First, SlicitoBasicBlock Last)> _roslynToSlicitoBasicBlocksMap = [];
    private readonly Dictionary<ISymbol, Variable> _variableMap = [];
    private readonly List<Variable> _temporaryVariables = [];
    private readonly FlowGraph.Builder _builder;
    private readonly OperationMapping.Builder _operationMappingBuilder;
    private readonly Variable? _returnVariable;

    private FlowGraphCreator(
        IMethodSymbol methodSymbol,
        ControlFlowGraph roslynCfg,
        Project project,
        ElementCache elementCache,
        OperationMapping.Builder operationMappingBuilder)
    {
        _roslynCfg = roslynCfg;
        _project = project;
        _elementCache = elementCache;
        _operationMappingBuilder = operationMappingBuilder;

        IEnumerable<Variable> instanceEnumerable =
            methodSymbol.IsStatic
            ? []
            : [CreateTemporaryVariable(TypeCreator.Create(methodSymbol.ContainingType))];

        var parameters = instanceEnumerable
            .Concat(methodSymbol.Parameters.Select(GetOrCreateVariable))
            .ToImmutableArray();

        ImmutableArray<Expression> returnValues;
        if (methodSymbol.ReturnType.SpecialType == SpecialType.System_Void)
        {
            returnValues = [];
        }
        else
        {
            _returnVariable = CreateTemporaryVariable(TypeCreator.Create(methodSymbol.ReturnType));

            returnValues = [new Expression.VariableReference(_returnVariable)];
        }

        _builder = new(parameters, returnValues);
    }

    public static (FlowGraphGroup FlowGraphGroup, OperationMapping OperationMapping)? TryCreate(
        IMethodSymbol method,
        Project project,
        ElementCache elementCache)
    {
        var roslynCfg = TryCreateRoslynControlFlowGraph(method, project);
        if (roslynCfg is null)
        {
            return null;
        }

        var operationMappingBuilder = new OperationMapping.Builder(ElementIdProvider.GetOperationIdPrefix(project, method));

        var rootFlowGraph = new FlowGraphCreator(method, roslynCfg, project, elementCache, operationMappingBuilder)
            .CreateFlowGraph();

        var elementIdToNestedFlowGraphBuilder = ImmutableDictionary.CreateBuilder<ElementId, IFlowGraph>();

        CreateNestedFlowGraphsRecursively(roslynCfg, project, elementCache, operationMappingBuilder, elementIdToNestedFlowGraphBuilder);

        return (new(rootFlowGraph, elementIdToNestedFlowGraphBuilder.ToImmutable()), operationMappingBuilder.Build());
    }

    private static void CreateNestedFlowGraphsRecursively(
        ControlFlowGraph roslynCfg,
        Project project,
        ElementCache elementCache,
        OperationMapping.Builder operationMappingBuilder,
        ImmutableDictionary<ElementId, IFlowGraph>.Builder elementIdToNestedFlowGraphBuilder)
    {
        var localFunctionsWithCfgs = roslynCfg.LocalFunctions
            .Select(localFunction => (localFunction, roslynCfg.GetLocalFunctionControlFlowGraph(localFunction)));

        var lambdasWithCfgs = LambdaFinder.FindLambdas(roslynCfg)
            .Select(lambda => (lambda.Symbol, roslynCfg.GetAnonymousFunctionControlFlowGraph(lambda)));

        foreach (var (nestedProcedure, nestedRoslynCfg) in localFunctionsWithCfgs.Concat(lambdasWithCfgs))
        {
            var flowGraph = new FlowGraphCreator(nestedProcedure, nestedRoslynCfg, project, elementCache, operationMappingBuilder)
                .CreateFlowGraph();

            var nestedProcedureElementTask = elementCache.GetElementAsync(project, nestedProcedure);
            if (!nestedProcedureElementTask.IsCompleted)
            {
                throw new InvalidOperationException($"Storing nested procedure '{nestedProcedure.Name}' in cache was unexpectedly asynchronous.");
            }

            elementIdToNestedFlowGraphBuilder[nestedProcedureElementTask.Result.Id] = flowGraph;

            CreateNestedFlowGraphsRecursively(nestedRoslynCfg, project, elementCache, operationMappingBuilder, elementIdToNestedFlowGraphBuilder);
        }
    }

    private static ControlFlowGraph? TryCreateRoslynControlFlowGraph(IMethodSymbol method, Project project)
    {
        var location = method.Locations.FirstOrDefault();
        if (location is null || !location.IsInSource)
        {
            return null;
        }

        var syntaxTree = location.SourceTree;
        var syntaxNode = syntaxTree?.GetRoot().FindNode(location.SourceSpan);
        if (syntaxTree is null || syntaxNode is null)
        {
            return null;
        }

        while (syntaxNode.Parent is not null &&
            syntaxNode is not (
                MethodDeclarationSyntax or
                ConstructorDeclarationSyntax or
                DestructorDeclarationSyntax or
                ConversionOperatorDeclarationSyntax or
                OperatorDeclarationSyntax or
                BlockSyntax or
                ArrowExpressionClauseSyntax))
        {
            syntaxNode = syntaxNode.Parent;
        }

        if (!project.TryGetCompilation(out var compilation))
        {
            return null;
        }

        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        return ControlFlowGraph.Create(syntaxNode, semanticModel);
    }

    private IFlowGraph CreateFlowGraph()
    {
        var entryBlock = _roslynCfg.Blocks.First();
        var exitBlock = _roslynCfg.Blocks.Last();

        Debug.Assert(entryBlock.Kind == BasicBlockKind.Entry);
        Debug.Assert(exitBlock.Kind == BasicBlockKind.Exit);

        _roslynToSlicitoBasicBlocksMap[entryBlock] = (_builder.Entry, _builder.Entry);
        _roslynToSlicitoBasicBlocksMap[exitBlock] = (_builder.Exit, _builder.Exit);

        foreach (var block in _roslynCfg.Blocks)
        {
            if (block == entryBlock || block == exitBlock)
            {
                continue;
            }

            _roslynToSlicitoBasicBlocksMap[block] = CreateBlockRange(block);
        }

        foreach (var block in _roslynCfg.Blocks)
        {
            CreateEdges(block);
        }

        return _builder.Build();
    }

    private (SlicitoBasicBlock First, SlicitoBasicBlock Last) CreateBlockRange(RoslynBasicBlock roslynBlock)
    {
        var context = new BlockTranslationContext(this);
        var creator = new OperationCreator(context);

        foreach (var roslynOperation in roslynBlock.Operations)
        {
            creator.Visit(roslynOperation, default);
        }

        Operation? additionalOperation = null;
        if (roslynBlock.ConditionalSuccessor is not null)
        {
            var conditionOperation = roslynBlock.BranchValue
                ?? throw new InvalidOperationException("Block with a conditional successor must have a branch value.");

            var condition = creator.Visit(conditionOperation, default)
                ?? throw new InvalidOperationException("Unexpectedly produced empty condition operation.");

            additionalOperation = new Operation.ConditionalJump(condition);

            _operationMappingBuilder.AddOperation(additionalOperation, conditionOperation.Syntax, context.ExtractAdditionalLinks());
        }
        else if (roslynBlock.FallThroughSuccessor?.Semantics == ControlFlowBranchSemantics.Return)
        {
            if (_returnVariable is not null)
            {
                var returnValue = creator.Visit(roslynBlock.BranchValue, default)
                    ?? throw new InvalidOperationException("Unexpectedly produced empty return value.");

                additionalOperation = new Operation.Assignment(
                    new SlicitoLocation.VariableReference(_returnVariable),
                    returnValue);

                _operationMappingBuilder.AddOperation(additionalOperation, roslynBlock.BranchValue!.Syntax, context.ExtractAdditionalLinks());
            }
            else if (roslynBlock.BranchValue is not null)
            {
                throw new InvalidOperationException("Unexpected return value in a void method.");
            }
        }

        foreach (var (operation, syntax, additionalLinks) in context.InnerOperations)
        {
            _operationMappingBuilder.AddOperation(operation, syntax, additionalLinks);
        }

        SlicitoBasicBlock.Inner? firstBlock = null;
        SlicitoBasicBlock.Inner? lastBlock = null;
        if (context.InnerOperations.Count > 0)
        {
            firstBlock = new SlicitoBasicBlock.Inner(context.InnerOperations[0].Operation);
            _builder.AddBlock(firstBlock);

            lastBlock = firstBlock;
            for (var i = 1; i < context.InnerOperations.Count; i++)
            {
                var currentBlock = new SlicitoBasicBlock.Inner(context.InnerOperations[i].Operation);
                _builder.AddBlock(currentBlock);
                _builder.AddUnconditionalEdge(lastBlock, currentBlock);
                lastBlock = currentBlock;
            }
        }

        if (firstBlock is null)
        {
            var onlyBlock = new SlicitoBasicBlock.Inner(additionalOperation);
            _builder.AddBlock(onlyBlock);
            firstBlock = onlyBlock;
            lastBlock = onlyBlock;
        }
        else if (additionalOperation is not null)
        {
            Debug.Assert(lastBlock is not null);

            var additionalBlock = new SlicitoBasicBlock.Inner(additionalOperation);
            _builder.AddBlock(additionalBlock);
            _builder.AddUnconditionalEdge(lastBlock!, additionalBlock);
            lastBlock = additionalBlock;
        }

        Debug.Assert(lastBlock is not null);

        return (firstBlock, lastBlock!);
    }

    private void CreateEdges(RoslynBasicBlock roslynBlock)
    {
        var (_, lastBlock) = _roslynToSlicitoBasicBlocksMap[roslynBlock];

        if (lastBlock is SlicitoBasicBlock.Inner { Operation: Operation.ConditionalJump })
        {
            var roslynConditionalSuccessor = roslynBlock.ConditionalSuccessor?.Destination
                ?? throw new InvalidOperationException("Block with a conditional jump must have a conditional successor.");

            var conditionalSuccessor = _roslynToSlicitoBasicBlocksMap[roslynConditionalSuccessor].First;

            var roslynFallThroughSuccessor = roslynBlock.FallThroughSuccessor?.Destination;
            if (roslynFallThroughSuccessor is null && 
                roslynBlock.FallThroughSuccessor?.Semantics != ControlFlowBranchSemantics.StructuredExceptionHandling)
            {
                throw new InvalidOperationException(
                    "Block with a conditional jump must have a fall-through successor unless it's the last block of finally or filter region.");
            }

            var fallThroughSuccessor =
                roslynFallThroughSuccessor is not null
                ? _roslynToSlicitoBasicBlocksMap[roslynFallThroughSuccessor].First
                : null;

            if (roslynBlock.ConditionKind == ControlFlowConditionKind.WhenTrue)
            {
                _builder.AddTrueEdge(lastBlock, conditionalSuccessor);

                if (fallThroughSuccessor is not null)
                {
                    _builder.AddFalseEdge(lastBlock, fallThroughSuccessor);
                }
            }
            else
            {
                Debug.Assert(roslynBlock.ConditionKind == ControlFlowConditionKind.WhenFalse);

                _builder.AddFalseEdge(lastBlock, conditionalSuccessor);

                if (fallThroughSuccessor is not null)
                {
                    _builder.AddTrueEdge(lastBlock, fallThroughSuccessor);
                }
            }
        }
        else
        {
            var fallThroughSuccessor = roslynBlock.FallThroughSuccessor?.Destination;
            if (fallThroughSuccessor is not null)
            {
                _builder.AddUnconditionalEdge(lastBlock, _roslynToSlicitoBasicBlocksMap[fallThroughSuccessor].First);
            }
        }
    }

    private Variable GetOrCreateVariable(ILocalSymbol local) => GetOrCreateVariable(local, local.Type);

    private Variable GetOrCreateVariable(IParameterSymbol parameter) => GetOrCreateVariable(parameter, parameter.Type);

    private Variable CreateTemporaryVariable(DataType type)
    {
        var variable = new Variable($"!tmp_{_temporaryVariables.Count}", type);
        _temporaryVariables.Add(variable);

        return variable;
    }

    private Variable GetOrCreateVariable(ISymbol variableSymbol, ITypeSymbol typeSymbol)
    {
        if (_variableMap.TryGetValue(variableSymbol, out var variable))
        {
            return variable;
        }

        variable = new Variable(variableSymbol.Name, TypeCreator.Create(typeSymbol));
        _variableMap[variableSymbol] = variable;
        return variable;
    }

    public class BlockTranslationContext(FlowGraphCreator creator)
    {
        private record PendingAdditionalLinks(List<ElementInfo> CallTargets)
        {
            public OperationMapping.AdditionalLinks ToAdditionalLinks() => new([.. CallTargets]);
        }

        private List<(Operation, SyntaxNode, OperationMapping.AdditionalLinks?)>? _innerOperations;
        private PendingAdditionalLinks? _pendingAdditionalLinks;

        public IReadOnlyList<(Operation Operation, SyntaxNode Syntax, OperationMapping.AdditionalLinks?)> InnerOperations => _innerOperations ?? [];

        public void AddCallTarget(ElementInfo callTarget)
        {
            _pendingAdditionalLinks ??= new([]);
            _pendingAdditionalLinks.CallTargets.Add(callTarget);
        }

        public void AddInnerOperation(Operation operation, SyntaxNode syntax)
        {
            _innerOperations ??= [];
            _innerOperations.Add((operation, syntax, ExtractAdditionalLinks()));
        }

        public OperationMapping.AdditionalLinks? ExtractAdditionalLinks()
        {
            var additionalLinks = _pendingAdditionalLinks;
            _pendingAdditionalLinks = null;
            return additionalLinks?.ToAdditionalLinks();
        }

        public Variable GetOrCreateVariable(ILocalSymbol local) => creator.GetOrCreateVariable(local);

        public Variable GetOrCreateVariable(IParameterSymbol parameter) => creator.GetOrCreateVariable(parameter);

        public Variable CreateTemporaryVariable(DataType type) => creator.CreateTemporaryVariable(type);

        // FIXME: The blocking call is bad, but turning the whole creator into async would be a large change.
        //        A reasonable solution would be to pre-compute all contained elements in the first step which would be async.
        public ElementInfo GetElement(ISymbol symbol) => creator._elementCache.GetElementAsync(creator._project, symbol).Result;
    }
}
