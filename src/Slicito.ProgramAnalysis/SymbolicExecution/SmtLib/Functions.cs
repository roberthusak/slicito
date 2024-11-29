namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

public static class Functions
{
    public static Function Not { get; } = new Function.Unary("not", Sorts.Bool, Sorts.Bool, IsBuiltIn: true);

    public static Function Implies { get; } = new Function.Binary("=>", Sorts.Bool, Sorts.Bool, Sorts.Bool, IsBuiltIn: true);

    public static Function And { get; } = new Function.Binary("and", Sorts.Bool, Sorts.Bool, Sorts.Bool, IsBuiltIn: true);

    public static Function Or { get; } = new Function.Binary("or", Sorts.Bool, Sorts.Bool, Sorts.Bool, IsBuiltIn: true);

    public static Function Xor { get; } = new Function.Binary("xor", Sorts.Bool, Sorts.Bool, Sorts.Bool, IsBuiltIn: true);

    public static Function Equals(Sort sort) => new Function.Binary("=", sort, sort, Sorts.Bool, IsBuiltIn: true);

    public static Function Distinct(Sort sort) => new Function.Binary("distinct", sort, sort, Sorts.Bool, IsBuiltIn: true);

    public static Function IfThenElse(Sort sort) => new Function.Ternary("ite", Sorts.Bool, sort, sort, sort, IsBuiltIn: true);

    public static class BitVec
    {
        public static Function Negate(int width) => new Function.Unary("bvneg", Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function Add(int width) => new Function.Binary("bvadd", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function Subtract(int width) => new Function.Binary("bvsub", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function Multiply(int width) => new Function.Binary("bvmul", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function UnsignedRemainder(int width) => new Function.Binary("bvurem", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function SignedRemainder(int width) => new Function.Binary("bvsrem", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function UnsignedDivide(int width) => new Function.Binary("bvudiv", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function SignedDivide(int width) => new Function.Binary("bvsdiv", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function UnsignedModulo(int width) => new Function.Binary("bvumod", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function SignedModulo(int width) => new Function.Binary("bvsmod", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function ShiftLeft(int width) => new Function.Binary("bvshl", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function LogicalShiftRight(int width) => new Function.Binary("bvlshr", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function ArithmeticShiftRight(int width) => new Function.Binary("bvashr", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function BitwiseNot(int width) => new Function.Unary("bvnot", Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function BitwiseAnd(int width) => new Function.Binary("bvand", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function BitwiseOr(int width) => new Function.Binary("bvor", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function BitwiseNand(int width) => new Function.Binary("bvnand", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function BitwiseNor(int width) => new Function.Binary("bvnor", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function BitwiseXnor(int width) => new Function.Binary("bvxnor", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function Concat(int width1, int width2) => new Function.Binary("concat", Sorts.BitVec(width1), Sorts.BitVec(width2), Sorts.BitVec(width1 + width2), IsBuiltIn: true);

        public static Function ZeroExtend(int width, int extension) => new Function.Unary($"(_ zero_extend {extension})", Sorts.BitVec(width), Sorts.BitVec(width + extension), IsBuiltIn: true);

        public static Function SignExtend(int width, int extension) => new Function.Unary($"(_ sign_extend {extension})", Sorts.BitVec(width), Sorts.BitVec(width + extension), IsBuiltIn: true);

        public static Function Extract(int width, int high, int low) => new Function.Unary($"(_ extract {high} {low})", Sorts.BitVec(width), Sorts.BitVec(high - low + 1), IsBuiltIn: true);

        public static Function RotateLeft(int width, int rotation) => new Function.Unary($"(_ rotate_left {rotation})", Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function RotateRight(int width, int rotation) => new Function.Unary($"(_ rotate_right {rotation})", Sorts.BitVec(width), Sorts.BitVec(width), IsBuiltIn: true);

        public static Function Repeat(int width, int count) => new Function.Unary($"(_ repeat {count})", Sorts.BitVec(width), Sorts.BitVec(width * count), IsBuiltIn: true);

        public static Function UnsignedLessOrEqual(int width) => new Function.Binary("bvule", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.Bool, IsBuiltIn: true);

        public static Function SignedLessOrEqual(int width) => new Function.Binary("bvsle", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.Bool, IsBuiltIn: true);

        public static Function UnsignedLessThan(int width) => new Function.Binary("bvult", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.Bool, IsBuiltIn: true);

        public static Function SignedLessThan(int width) => new Function.Binary("bvslt", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.Bool, IsBuiltIn: true);

        public static Function UnsignedGreaterOrEqual(int width) => new Function.Binary("bvuge", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.Bool, IsBuiltIn: true);

        public static Function SignedGreaterOrEqual(int width) => new Function.Binary("bvuge", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.Bool, IsBuiltIn: true);

        public static Function UnsignedGreaterThan(int width) => new Function.Binary("bvugt", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.Bool, IsBuiltIn: true);

        public static Function SignedGreaterThan(int width) => new Function.Binary("bvsgt", Sorts.BitVec(width), Sorts.BitVec(width), Sorts.Bool, IsBuiltIn: true);
    }
}
