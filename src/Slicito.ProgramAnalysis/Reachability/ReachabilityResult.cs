using System.Collections.Immutable;

using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis.Reachability;

public abstract record ReachabilityResult
{
    private ReachabilityResult() { }

    public sealed record Reachable(ImmutableDictionary<Variable, Expression.Constant> Assignments) : ReachabilityResult;

    public sealed record Unreachable() : ReachabilityResult;

    public sealed record Unknown() : ReachabilityResult;
}
