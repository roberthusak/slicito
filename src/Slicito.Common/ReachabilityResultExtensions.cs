using System.Collections.Immutable;

using Slicito.Abstractions.Models;
using Slicito.ProgramAnalysis.Notation;
using Slicito.ProgramAnalysis.Reachability;

namespace Slicito.Common;

public static class ReachabilityResultExtensions
{
    public static Tree ToTree(this ReachabilityResult result)
    {
        return result switch
        {
            ReachabilityResult.Reachable(var assignments) => new([new("Inputs:", [.. AssignmentsToTreeItems(assignments)])]),
            ReachabilityResult.Unreachable => new([new("Unreachable", [])]),
            ReachabilityResult.Unknown => new([new("Unknown", [])]),
            _ => throw new ArgumentException($"Unsupported reachability result type {result.GetType().Name}.")
        };
    }

    private static IEnumerable<TreeItem> AssignmentsToTreeItems(ImmutableDictionary<Variable, Expression.Constant> assignments)
    {
        foreach (var kvp in assignments)
        {
            yield return new($"{kvp.Key.Name}: {kvp.Value.Format()}", []);
        }
    }
}