using System.Collections.Immutable;

using Slicito.Abstractions.Models;
using Slicito.ProgramAnalysis.Notation;
using Slicito.ProgramAnalysis.Reachability;

namespace Slicito.Common.Models;

public class ReachabilityResultModelCreator : IModelCreator<ReachabilityResult>
{
    public IModel CreateModel(ReachabilityResult value)
    {
        return value switch
        {
            ReachabilityResult.Reachable reachable => new Tree([new("Inputs:", ListInputAssignments(reachable.Assignments))]),
            ReachabilityResult.Unreachable unreachable => new Tree([new("Unreachable", [])]),
            ReachabilityResult.Unknown unknown => new Tree([new("Unknown", [])]),

            _ => throw new NotSupportedException($"Unsupported reachability result: {value}"),
        };
    }

    private ImmutableArray<TreeItem> ListInputAssignments(ImmutableDictionary<Variable, Expression.Constant> assignments)
    {
        return assignments
            .Select(assignment =>
                new TreeItem($"{assignment.Key.Name}: {assignment.Value.Format()}", []))
            .ToImmutableArray();
    }
}
