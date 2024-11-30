using System.Collections.Immutable;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

using Slicito.ProgramAnalysis.Notation;

using RoslynBasicBlock = Microsoft.CodeAnalysis.FlowAnalysis.BasicBlock;
using SlicitoBasicBlock = Slicito.ProgramAnalysis.Notation.BasicBlock;

namespace Slicito.DotNet.Implementation;

internal class FlowGraphCreator
{
    private readonly ControlFlowGraph _roslynCfg;

    private readonly Dictionary<RoslynBasicBlock, (SlicitoBasicBlock First, SlicitoBasicBlock Last)> _roslynToSlicitoBasicBlocksMap = [];
    private readonly Dictionary<ISymbol, Variable> _variableMap = [];
    private readonly FlowGraph.Builder _builder;

    private FlowGraphCreator(ControlFlowGraph roslynCfg, ImmutableArray<IParameterSymbol> parameterSymbols)
    {
        _roslynCfg = roslynCfg;
        _builder = new(parameterSymbols.Select(GetOrCreateVariable).ToImmutableArray());
    }

    public static IFlowGraph? TryCreateFlowGraph(IMethodSymbol method, Solution solution)
    {
        var roslynCfg = TryCreateRoslynControlFlowGraph(method, solution);
        if (roslynCfg is null)
        {
            return null;
        }

        var creator = new FlowGraphCreator(roslynCfg, method.Parameters);
        return creator.CreateFlowGraph();
    }

    private static ControlFlowGraph? TryCreateRoslynControlFlowGraph(IMethodSymbol method, Solution solution)
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

        var compilation = solution.Projects
            .Select(p =>
                p.TryGetCompilation(out var compilation) ? compilation : null)
            .FirstOrDefault(c =>
                method.ContainingAssembly.Equals(c?.Assembly, SymbolEqualityComparer.Default));

        if (compilation is null)
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
        }

        SlicitoBasicBlock.Inner? firstBlock = null;
        SlicitoBasicBlock.Inner? lastBlock = null;
        if (context.InnerOperations.Count > 0)
        {
            firstBlock = new SlicitoBasicBlock.Inner(context.InnerOperations[0]);
            _builder.AddBlock(firstBlock);

            lastBlock = firstBlock;
            for (var i = 1; i < context.InnerOperations.Count; i++)
            {
                var currentBlock = new SlicitoBasicBlock.Inner(context.InnerOperations[i]);
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

            var roslynFallThroughSuccessor = roslynBlock.FallThroughSuccessor?.Destination
                ?? throw new InvalidOperationException("Block with a conditional jump must have a fall-through successor.");

            var fallThroughSuccessor = _roslynToSlicitoBasicBlocksMap[roslynFallThroughSuccessor].First;

            if (roslynBlock.ConditionKind == ControlFlowConditionKind.WhenTrue)
            {
                _builder.AddTrueEdge(lastBlock, conditionalSuccessor);
                _builder.AddFalseEdge(lastBlock, fallThroughSuccessor);
            }
            else
            {
                Debug.Assert(roslynBlock.ConditionKind == ControlFlowConditionKind.WhenFalse);

                _builder.AddFalseEdge(lastBlock, conditionalSuccessor);
                _builder.AddTrueEdge(lastBlock, fallThroughSuccessor);
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
        private List<Operation>? _innerOperations;

        public IReadOnlyList<Operation> InnerOperations => _innerOperations ?? [];

        public void AddInnerOperation(Operation operation)
        {
            _innerOperations ??= [];
            _innerOperations.Add(operation);
        }

        internal Variable GetOrCreateVariable(ILocalSymbol local) => creator.GetOrCreateVariable(local);

        internal Variable GetOrCreateVariable(IParameterSymbol parameter) => creator.GetOrCreateVariable(parameter);
    }
}
