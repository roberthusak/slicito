using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Slicito.ProgramAnalysis.Notation;

namespace Slicito.DotNet.Implementation;

internal static class ProcedureSignatureCreator
{
    public static ProcedureSignature Create(IMethodSymbol method)
    {
        // Ensure uniqueness and simplify mapping back to the element
        var id = ElementIdProvider.GetId(method);

        var parameterTypes = method.Parameters
            .Select(p => TypeCreator.Create(p.Type))
            .ToImmutableArray();

        var returnType = method.ReturnType.SpecialType == SpecialType.System_Void
            ? ImmutableArray<DataType>.Empty
            : [TypeCreator.Create(method.ReturnType)];

        return new(id.Value, parameterTypes, returnType);
    }
}
