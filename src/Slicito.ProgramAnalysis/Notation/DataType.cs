namespace Slicito.ProgramAnalysis.Notation;

public abstract record DataType
{
    private DataType() { }

    public sealed record Integer(bool Signed, int Bits) : DataType;

    public sealed record Float(int ExponentBits, int MantissaBits) : DataType;

    public sealed record Boolean() : DataType;
}
