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

    public static class Int
    {
        public static Function Negate { get; } = new Function.Unary("-", Sorts.Int, Sorts.Int, IsBuiltIn: true);

        public static Function Subtract { get; } = new Function.Binary("-", Sorts.Int, Sorts.Int, Sorts.Int, IsBuiltIn: true);

        public static Function Add { get; } = new Function.Binary("+", Sorts.Int, Sorts.Int, Sorts.Int, IsBuiltIn: true);

        public static Function Multiply { get; } = new Function.Binary("*", Sorts.Int, Sorts.Int, Sorts.Int, IsBuiltIn: true);

        public static Function Divide { get; } = new Function.Binary("/", Sorts.Int, Sorts.Int, Sorts.Int, IsBuiltIn: true);

        public static Function Modulo { get; } = new Function.Binary("mod", Sorts.Int, Sorts.Int, Sorts.Int, IsBuiltIn: true);

        public static Function AbsoluteValue { get; } = new Function.Unary("abs", Sorts.Int, Sorts.Int, IsBuiltIn: true);

        public static Function LessThanOrEqual { get; } = new Function.Binary("<=", Sorts.Int, Sorts.Int, Sorts.Bool, IsBuiltIn: true);

        public static Function LessThan { get; } = new Function.Binary("<", Sorts.Int, Sorts.Int, Sorts.Bool, IsBuiltIn: true);

        public static Function GreaterThanOrEqual { get; } = new Function.Binary(">=", Sorts.Int, Sorts.Int, Sorts.Bool, IsBuiltIn: true);

        public static Function GreaterThan { get; } = new Function.Binary(">", Sorts.Int, Sorts.Int, Sorts.Bool, IsBuiltIn: true);

        public static Function DivisibleBy(ulong divisor) => new Function.Unary($"(_ divisible {divisor})", Sorts.Int, Sorts.Bool, IsBuiltIn: true);

        public static Function ToBitVec(int width) => new Function.Unary($"(_ int2bv {width})", Sorts.Int, Sorts.BitVec(width), IsBuiltIn: true);
    }

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

        public static Function ToNatural(int width) => new Function.Unary("bv2nat", Sorts.BitVec(width), Sorts.Int, IsBuiltIn: true);
    }

    public static class String
    {
        public static Function Concatenate { get; } = new Function.Binary("str.++", Sorts.String, Sorts.String, Sorts.String, IsBuiltIn: true);

        public static Function Length { get; } = new Function.Unary("str.len", Sorts.String, Sorts.Int, IsBuiltIn: true);

        public static Function IsLexicographicallyLessThan { get; } = new Function.Binary("str.<", Sorts.String, Sorts.String, Sorts.Bool, IsBuiltIn: true);

        public static Function IsLexicographicallyLessThanOrEqual { get; } = new Function.Binary("str.<=", Sorts.String, Sorts.String, Sorts.Bool, IsBuiltIn: true);

        public static Function At { get; } = new Function.Binary("str.at", Sorts.String, Sorts.Int, Sorts.String, IsBuiltIn: true);

        public static Function Substring { get; } = new Function.Ternary("str.substr", Sorts.String, Sorts.Int, Sorts.Int, Sorts.String, IsBuiltIn: true);

        public static Function IsPrefixOf { get; } = new Function.Binary("str.prefixof", Sorts.String, Sorts.String, Sorts.Bool, IsBuiltIn: true);

        public static Function IsSuffixOf { get; } = new Function.Binary("str.suffixof", Sorts.String, Sorts.String, Sorts.Bool, IsBuiltIn: true);

        public static Function Contains { get; } = new Function.Binary("str.contains", Sorts.String, Sorts.String, Sorts.Bool, IsBuiltIn: true);

        public static Function IndexOf { get; } = new Function.Ternary("str.indexof", Sorts.String, Sorts.String, Sorts.Int, Sorts.Int, IsBuiltIn: true);

        public static Function Replace { get; } = new Function.Ternary("str.replace", Sorts.String, Sorts.String, Sorts.String, Sorts.String, IsBuiltIn: true);

        public static Function ReplaceAll { get; } = new Function.Ternary("str.replace_all", Sorts.String, Sorts.String, Sorts.String, Sorts.String, IsBuiltIn: true);

        public static Function ReplaceRegLan { get; } = new Function.Ternary("str.replace_re", Sorts.String, Sorts.RegLan, Sorts.String, Sorts.String, IsBuiltIn: true);

        public static Function ReplaceRegLanAll { get; } = new Function.Ternary("str.replace_re_all", Sorts.String, Sorts.RegLan, Sorts.String, Sorts.String, IsBuiltIn: true);

        public static Function IsDigit { get; } = new Function.Unary("str.is_digit", Sorts.String, Sorts.Bool, IsBuiltIn: true);

        public static Function ToCode { get; } = new Function.Unary("str.to_code", Sorts.String, Sorts.Int, IsBuiltIn: true);

        public static Function FromCode { get; } = new Function.Unary("str.from_code", Sorts.Int, Sorts.String, IsBuiltIn: true);

        public static Function ToInt { get; } = new Function.Unary("str.to_int", Sorts.String, Sorts.Int, IsBuiltIn: true);

        public static Function FromInt { get; } = new Function.Unary("str.from_int", Sorts.Int, Sorts.String, IsBuiltIn: true);

        public static Function ToRegLan { get; } = new Function.Unary("str.to_re", Sorts.String, Sorts.RegLan, IsBuiltIn: true);

        public static Function IsInRegLan { get; } = new Function.Binary("str.in_re", Sorts.String, Sorts.RegLan, Sorts.Bool, IsBuiltIn: true);
    }

    public static class RegLan
    {
        public static Function None { get; } = new Function.Nullary("re.none", Sorts.RegLan, IsBuiltIn: true);

        public static Function All { get; } = new Function.Nullary("re.all", Sorts.RegLan, IsBuiltIn: true);

        public static Function AllCharacters { get; } = new Function.Nullary("re.allchar", Sorts.RegLan, IsBuiltIn: true);

        public static Function Concatenate { get; } = new Function.Binary("re.++", Sorts.RegLan, Sorts.RegLan, Sorts.RegLan, IsBuiltIn: true);

        public static Function Union { get; } = new Function.Binary("re.union", Sorts.RegLan, Sorts.RegLan, Sorts.RegLan, IsBuiltIn: true);

        public static Function Intersection { get; } = new Function.Binary("re.inter", Sorts.RegLan, Sorts.RegLan, Sorts.RegLan, IsBuiltIn: true);

        public static Function KleeneStar { get; } = new Function.Unary("re.*", Sorts.RegLan, Sorts.RegLan, IsBuiltIn: true);

        public static Function Complement { get; } = new Function.Unary("re.comp", Sorts.RegLan, Sorts.RegLan, IsBuiltIn: true);

        public static Function Difference { get; } = new Function.Binary("re.diff", Sorts.RegLan, Sorts.RegLan, Sorts.RegLan, IsBuiltIn: true);

        public static Function KleenePlus { get; } = new Function.Unary("re.+", Sorts.RegLan, Sorts.RegLan, IsBuiltIn: true);

        public static Function Optional { get; } = new Function.Unary("re.opt", Sorts.RegLan, Sorts.RegLan, IsBuiltIn: true);

        public static Function Range { get; } = new Function.Binary("re.range", Sorts.String, Sorts.String, Sorts.RegLan, IsBuiltIn: true);

        public static Function Power(int n) => new Function.Unary($"(_ re.^ {n})", Sorts.RegLan, Sorts.RegLan, IsBuiltIn: true);

        public static Function Loop(int n1, int n2) => new Function.Unary($"(_ re.loop {n1} {n2})", Sorts.RegLan, Sorts.RegLan, IsBuiltIn: true);
    }
}
