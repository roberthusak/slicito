using System.Collections.Immutable;

using Slicito.Common;
using Slicito.ProgramAnalysis.Notation;

namespace Controllers;

public static class BlockLabelProvider
{
    public static string GetLabel(BasicBlock block)
    {
        return block switch
        {
            BasicBlock.Entry entry => $"Entry({FormatParameters(entry.Parameters)})",
            BasicBlock.Exit exit => $"Exit({FormatReturnValues(exit.ReturnValues)})",
            BasicBlock.Inner inner => GetInnerBlockLabel(inner),
            _ => throw new ArgumentException($"Unsupported block type {block.GetType().Name}.")
        };
    }

    private static string FormatParameters(ImmutableArray<Variable> parameters) =>
        string.Join(", ", parameters.Select(p => p.Name));

    private static string FormatReturnValues(ImmutableArray<Expression> returnValues) =>
        string.Join(", ", returnValues.Select(e => e.Format()));

    private static string GetInnerBlockLabel(BasicBlock.Inner block)
    {
        if (block.Operation is null)
        {
            return "Empty";
        }

        return block.Operation switch
        {
            Operation.Assignment assignment => FormatAssignment(assignment),
            Operation.ConditionalJump condition => condition.Condition.Format(),
            Operation.Call call => FormatCall(call),
            _ => throw new ArgumentException($"Unsupported operation type {block.Operation.GetType().Name}.")
        };
    }

    private static string FormatAssignment(Operation.Assignment assignment)
    {
        var location = FormatLocation(assignment.Location);

        return $"{location} = {assignment.Value.Format()}";
    }

    private static string FormatCall(Operation.Call call)
    {
        var returnLocations = string.Join(", ", call.ReturnLocations.Select(loc => loc is null ? "_" : FormatLocation(loc)));
        var arguments = string.Join(", ", call.Arguments.Select(e => e.Format()));

        return $"({returnLocations}) = {call.Signature.Name}({arguments})";
    }

    private static string FormatLocation(Location location)
    {
        return location switch
        {
            Location.VariableReference varRef => varRef.Variable.Name,
            _ => throw new ArgumentException($"Unsupported location type {location.GetType().Name}.")
        };
    }
} 
