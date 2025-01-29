namespace Slicito.ProgramAnalysis.Notation;

public abstract record DataType
{
    private DataType() { }

    public sealed record Boolean() : DataType
    {
        public static Boolean Instance { get; } = new();
    }

    public sealed record Integer(bool Signed, int Bits) : DataType;

    public sealed record Float(int ExponentBits, int MantissaBits) : DataType;

    public sealed record Utf16String() : DataType
    {
        public static Utf16String Instance { get; } = new();
    }
}
