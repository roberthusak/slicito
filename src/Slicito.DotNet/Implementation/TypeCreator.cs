using Microsoft.CodeAnalysis;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.DotNet.Implementation;

internal static class TypeCreator
{
    public static DataType Create(ITypeSymbol typeSymbol) =>
        CreateOrNull(typeSymbol.SpecialType) ?? new DataType.Unsupported(typeSymbol.Name);

    public static DataType Create(SpecialType specialType) =>
        CreateOrNull(specialType) ?? throw new NotSupportedException($"Special type {specialType} is not supported");

    public static DataType? CreateOrNull(SpecialType specialType)
    {
        return specialType switch
        {
            SpecialType.System_Boolean => DataType.Boolean.Instance,
            
            SpecialType.System_SByte => new DataType.Integer(Signed: true, Bits: 8),
            SpecialType.System_Int16 => new DataType.Integer(Signed: true, Bits: 16),
            SpecialType.System_Int32 => new DataType.Integer(Signed: true, Bits: 32),
            SpecialType.System_Int64 => new DataType.Integer(Signed: true, Bits: 64),
            
            SpecialType.System_Byte => new DataType.Integer(Signed: false, Bits: 8),
            SpecialType.System_UInt16 => new DataType.Integer(Signed: false, Bits: 16),
            SpecialType.System_UInt32 => new DataType.Integer(Signed: false, Bits: 32),
            SpecialType.System_UInt64 => new DataType.Integer(Signed: false, Bits: 64),
            
            SpecialType.System_Single => new DataType.Float(ExponentBits: 8, MantissaBits: 24),
            SpecialType.System_Double => new DataType.Float(ExponentBits: 11, MantissaBits: 53),

            SpecialType.System_String => DataType.Utf16String.Instance,
            
            _ => null
        };
    }
}
