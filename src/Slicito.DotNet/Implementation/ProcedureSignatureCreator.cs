using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.DotNet.Implementation;

internal static class ProcedureSignatureCreator
{
    public static ProcedureSignature Create(IMethodSymbol method, ElementId id)
    {
        IEnumerable<DataType> instanceTypeEnumerable = method.IsStatic ? [] : [TypeCreator.Create(method.ContainingType)];

        var parameterTypes = instanceTypeEnumerable
            .Concat(method.Parameters.Select(p => TypeCreator.Create(p.Type)))
            .ToImmutableArray();

        var returnType = GetMethodReturnType(method);

        return new(id.Value, parameterTypes, returnType);
    }

    private static ImmutableArray<DataType> GetMethodReturnType(IMethodSymbol method)
    {
        if (method.MethodKind == MethodKind.Constructor)
        {
            return [TypeCreator.Create(method.ContainingType)];
        }
        else if (method.ReturnType.SpecialType == SpecialType.System_Void)
        {
            return ImmutableArray<DataType>.Empty;
        }
        else
        {
            return  [TypeCreator.Create(method.ReturnType)];
        }
    }
}
