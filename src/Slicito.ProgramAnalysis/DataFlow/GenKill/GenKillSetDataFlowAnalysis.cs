using System.Collections.Immutable;

using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis.DataFlow.GenKill;

public static class GenKillSetDataFlowAnalysis
{
    public static IDataFlowAnalysis<ImmutableHashSet<TElement>> Create<TElement>(IGenKillAnalysis<TElement> genKillAnalysis) =>
        new GenKillSetDataFlowAnalysis<TElement>(genKillAnalysis);
}

public sealed class GenKillSetDataFlowAnalysis<TElement>(IGenKillAnalysis<TElement> genKillAnalysis) : IDataFlowAnalysis<ImmutableHashSet<TElement>>
{
    public AnalysisDirection Direction => genKillAnalysis.Direction;

    public ImmutableHashSet<TElement> GetInitialInputValue(BasicBlock block) => [];

    public ImmutableHashSet<TElement> GetInitialOutputValue(BasicBlock block) => [];

    public void Initialize(IFlowGraph graph) => genKillAnalysis.Initialize(graph);

    public ImmutableHashSet<TElement> Transfer(BasicBlock block, ImmutableHashSet<TElement> value)
    {
        var gen = genKillAnalysis.GetGen(block);
        var kill = genKillAnalysis.GetKill(block);

        return value.Union(gen).Except(kill);
    }

    public ImmutableHashSet<TElement> Meet(ImmutableHashSet<TElement> left, ImmutableHashSet<TElement> right)
    {
        return genKillAnalysis.MeetVariant switch
        {
            GenKillMeetVariant.Union => left.Union(right),
            GenKillMeetVariant.Intersection => left.Intersect(right),
        };
    }

    public bool Equals(ImmutableHashSet<TElement> left, ImmutableHashSet<TElement> right) => left.SetEquals(right);
}
