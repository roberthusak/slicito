namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

public static class Sorts
{
    public static Sort.Bool Bool { get; } = new Sort.Bool();

    public static Sort.Int Int { get; } = new Sort.Int();

    public static Sort.BitVec BitVec8 { get; } = new Sort.BitVec(8);

    public static Sort.BitVec BitVec16 { get; } = new Sort.BitVec(16);

    public static Sort.BitVec BitVec32 { get; } = new Sort.BitVec(32);

    public static Sort.BitVec BitVec64 { get; } = new Sort.BitVec(64);

    public static Sort.BitVec BitVec(int width) => width switch
    {
        8 => BitVec8,
        16 => BitVec16,
        32 => BitVec32,
        64 => BitVec64,
        _ => new Sort.BitVec(width)
    };
}
