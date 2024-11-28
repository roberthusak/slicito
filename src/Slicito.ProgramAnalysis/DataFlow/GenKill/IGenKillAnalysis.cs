using System.Collections.Immutable;

using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis.DataFlow.GenKill;

public interface IGenKillAnalysis<TElement>
{
    AnalysisDirection Direction { get; }

    void Initialize(IFlowGraph graph);

    GenKillMeetVariant MeetVariant { get; }

    ImmutableHashSet<TElement> GetGen(BasicBlock block);

    ImmutableHashSet<TElement> GetKill(BasicBlock block);
}
