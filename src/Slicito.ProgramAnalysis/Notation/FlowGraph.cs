using System.Collections.Immutable;

namespace Slicito.ProgramAnalysis.Notation;

public sealed class FlowGraph : IFlowGraph
{
    private readonly ImmutableDictionary<BasicBlock, (BasicBlock? True, BasicBlock? False, BasicBlock? Unconditional)> _successors;
    private readonly ImmutableDictionary<BasicBlock, ImmutableHashSet<BasicBlock>> _predecessors;
    private readonly ImmutableHashSet<BasicBlock> _blocks;

    public BasicBlock.Entry Entry { get; }
    public BasicBlock.Exit Exit { get; }
    public IEnumerable<BasicBlock> Blocks => _blocks;

    private FlowGraph(
        BasicBlock.Entry entry,
        BasicBlock.Exit exit,
        ImmutableDictionary<BasicBlock, (BasicBlock? True, BasicBlock? False, BasicBlock? Unconditional)> successors,
        ImmutableDictionary<BasicBlock, ImmutableHashSet<BasicBlock>> predecessors,
        ImmutableHashSet<BasicBlock> blocks)
    {
        Entry = entry;
        Exit = exit;
        _successors = successors;
        _predecessors = predecessors;
        _blocks = blocks;
    }

    public BasicBlock? GetTrueSuccessor(BasicBlock block) =>
        _successors.TryGetValue(block, out var successors) ? successors.True : null;

    public BasicBlock? GetFalseSuccessor(BasicBlock block) =>
        _successors.TryGetValue(block, out var successors) ? successors.False : null;

    public BasicBlock? GetUnconditionalSuccessor(BasicBlock block) =>
        _successors.TryGetValue(block, out var successors) ? successors.Unconditional : null;

    public IEnumerable<BasicBlock> GetSuccessors(BasicBlock block)
    {
        if (_successors.TryGetValue(block, out var successors))
        {
            if (successors.True is not null)
            {
                yield return successors.True;
            }

            if (successors.False is not null)
            {
                yield return successors.False;
            }

            if (successors.Unconditional is not null)
            {
                yield return successors.Unconditional;
            }
        }
    }

    public IEnumerable<BasicBlock> GetPredecessors(BasicBlock block) =>
        _predecessors.TryGetValue(block, out var predecessors) ? predecessors : Enumerable.Empty<BasicBlock>();

    public FlowGraphBuilder ToBuilder() => new(this);

    public sealed class FlowGraphBuilder : IFlowGraphBuilder
    {
        private readonly Dictionary<BasicBlock, (BasicBlock? True, BasicBlock? False, BasicBlock? Unconditional)> _successors;
        private readonly Dictionary<BasicBlock, HashSet<BasicBlock>> _predecessors;
        private readonly HashSet<BasicBlock> _blocks;

        public FlowGraphBuilder()
        {
            _successors = new Dictionary<BasicBlock, (BasicBlock?, BasicBlock?, BasicBlock?)>();
            _predecessors = new Dictionary<BasicBlock, HashSet<BasicBlock>>();
            _blocks = new HashSet<BasicBlock>();
            Entry = new BasicBlock.Entry();
            Exit = new BasicBlock.Exit();
            
            _blocks.Add(Entry);
            _blocks.Add(Exit);
        }

        internal FlowGraphBuilder(FlowGraph graph)
        {
            _successors = graph._successors.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value);
            _predecessors = graph._predecessors.ToDictionary(
                kvp => kvp.Key,
                kvp => new HashSet<BasicBlock>(kvp.Value));
            _blocks = [.. graph._blocks];
            Entry = graph.Entry;
            Exit = graph.Exit;
        }

        public BasicBlock.Entry Entry { get; }
        public BasicBlock.Exit Exit { get; }
        public IEnumerable<BasicBlock> Blocks => _blocks;

        public void AddBlock(BasicBlock.Inner block)
        {
            _blocks.Add(block);
        }

        public void AddTrueEdge(BasicBlock source, BasicBlock target)
        {
            EnsureBlocksExist(source, target);
            if (!_successors.TryGetValue(source, out var successors))
            {
                successors = (null, null, null);
            }

            var (_, falseSucc, uncondSucc) = successors;
            _successors[source] = (target, falseSucc, uncondSucc);
            AddPredecessor(target, source);
        }

        public void AddFalseEdge(BasicBlock source, BasicBlock target)
        {
            EnsureBlocksExist(source, target);
            if (!_successors.TryGetValue(source, out var successors))
            {
                successors = (null, null, null);
            }

            var (trueSucc, _, uncondSucc) = successors;
            _successors[source] = (trueSucc, target, uncondSucc);
            AddPredecessor(target, source);
        }

        public void AddUnconditionalEdge(BasicBlock source, BasicBlock target)
        {
            EnsureBlocksExist(source, target);
            if (!_successors.TryGetValue(source, out var successors))
            {
                successors = (null, null, null);
            }

            var (trueSucc, falseSucc, _) = successors;
            _successors[source] = (trueSucc, falseSucc, target);
            AddPredecessor(target, source);
        }

        private void EnsureBlocksExist(BasicBlock source, BasicBlock target)
        {
            if (!_blocks.Contains(source))
            {
                throw new ArgumentException($"Source block {source} does not exist in the graph.", nameof(source));
            }

            if (!_blocks.Contains(target))
            {
                throw new ArgumentException($"Target block {target} does not exist in the graph.", nameof(target));
            }
        }

        private void AddPredecessor(BasicBlock target, BasicBlock source)
        {
            if (!_predecessors.TryGetValue(target, out var predecessors))
            {
                predecessors = new HashSet<BasicBlock>();
                _predecessors[target] = predecessors;
            }
            predecessors.Add(source);
        }

        public BasicBlock? GetTrueSuccessor(BasicBlock block) =>
            _successors.TryGetValue(block, out var successors) ? successors.True : null;

        public BasicBlock? GetFalseSuccessor(BasicBlock block) =>
            _successors.TryGetValue(block, out var successors) ? successors.False : null;

        public BasicBlock? GetUnconditionalSuccessor(BasicBlock block) =>
            _successors.TryGetValue(block, out var successors) ? successors.Unconditional : null;

        public IEnumerable<BasicBlock> GetSuccessors(BasicBlock block)
        {
            if (_successors.TryGetValue(block, out var successors))
            {
                if (successors.True is not null)
                {
                    yield return successors.True;
                }

                if (successors.False is not null)
                {
                    yield return successors.False;
                }

                if (successors.Unconditional is not null)
                {
                    yield return successors.Unconditional;
                }
            }
        }

        public IEnumerable<BasicBlock> GetPredecessors(BasicBlock block) =>
            _predecessors.TryGetValue(block, out var predecessors) ? predecessors : Enumerable.Empty<BasicBlock>();

        public FlowGraph Build() => new(
            Entry,
            Exit,
            _successors.ToImmutableDictionary(),
            _predecessors.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableHashSet()),
            _blocks.ToImmutableHashSet());

        IFlowGraph IFlowGraphBuilder.Build() => Build();
    }
} 