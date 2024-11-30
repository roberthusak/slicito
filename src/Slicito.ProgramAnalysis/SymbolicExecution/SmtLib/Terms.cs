namespace Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

public static class Terms
{
    public static Term.FunctionApplication Constant(Function.Nullary function) => new(function, []);

    public static Term.Constant.Bool True { get; } = new(true);

    public static Term.Constant.Bool False { get; } = new(false);

    public static Term.FunctionApplication Not(Term value) => new(Functions.Not, [value]);

    public static Term.FunctionApplication Implies(Term left, Term right) => new(Functions.Implies, [left, right]);

    public static Term.FunctionApplication And(Term left, Term right) => new(Functions.And, [left, right]);

    public static Term.FunctionApplication Or(Term left, Term right) => new(Functions.Or, [left, right]);

    public static Term.FunctionApplication Xor(Term left, Term right) => new(Functions.Xor, [left, right]);

    public static Term.FunctionApplication Equal(Term left, Term right) => new(Functions.Equals(left.Sort), [left, right]);

    public static Term.FunctionApplication Distinct(Term left, Term right) => new(Functions.Distinct(left.Sort), [left, right]);

    public static Term.FunctionApplication IfThenElse(Term condition, Term then, Term @else) => new(Functions.IfThenElse(then.Sort), [condition, then, @else]);

    public static class BitVec
    {
        public static Term.Constant.BitVec Literal(long value, Sort.BitVec bitVecSort) => new(value, bitVecSort);

        public static Term.FunctionApplication Negate(Term value) => new(Functions.BitVec.Negate(GetWidth(value.Sort)), [value]);

        public static Term.FunctionApplication Add(Term left, Term right) => new(Functions.BitVec.Add(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication Subtract(Term left, Term right) => new(Functions.BitVec.Subtract(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication Multiply(Term left, Term right) => new(Functions.BitVec.Multiply(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication UnsignedRemainder(Term left, Term right) => new(Functions.BitVec.UnsignedRemainder(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication SignedRemainder(Term left, Term right) => new(Functions.BitVec.SignedRemainder(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication UnsignedDivide(Term left, Term right) => new(Functions.BitVec.UnsignedDivide(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication SignedDivide(Term left, Term right) => new(Functions.BitVec.SignedDivide(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication UnsignedModulo(Term left, Term right) => new(Functions.BitVec.UnsignedModulo(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication SignedModulo(Term left, Term right) => new(Functions.BitVec.SignedModulo(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication ShiftLeft(Term value, Term shift) => new(Functions.BitVec.ShiftLeft(GetWidth(value.Sort)), [value, shift]);

        public static Term.FunctionApplication LogicalShiftRight(Term value, Term shift) => new(Functions.BitVec.LogicalShiftRight(GetWidth(value.Sort)), [value, shift]);

        public static Term.FunctionApplication ArithmeticShiftRight(Term value, Term shift) => new(Functions.BitVec.ArithmeticShiftRight(GetWidth(value.Sort)), [value, shift]);

        public static Term.FunctionApplication BitwiseNot(Term value) => new(Functions.BitVec.BitwiseNot(GetWidth(value.Sort)), [value]);

        public static Term.FunctionApplication BitwiseAnd(Term left, Term right) => new(Functions.BitVec.BitwiseAnd(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication BitwiseOr(Term left, Term right) => new(Functions.BitVec.BitwiseOr(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication BitwiseNand(Term left, Term right) => new(Functions.BitVec.BitwiseNand(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication BitwiseNor(Term left, Term right) => new(Functions.BitVec.BitwiseNor(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication BitwiseXnor(Term left, Term right) => new(Functions.BitVec.BitwiseXnor(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication Concat(Term left, Term right) => new(Functions.BitVec.Concat(GetWidth(left.Sort), GetWidth(right.Sort)), [left, right]);

        public static Term.FunctionApplication ZeroExtend(Term value, int extension) => new(Functions.BitVec.ZeroExtend(GetWidth(value.Sort), extension), [value]);

        public static Term.FunctionApplication SignExtend(Term value, int extension) => new(Functions.BitVec.SignExtend(GetWidth(value.Sort), extension), [value]);

        public static Term.FunctionApplication Extract(Term value, int high, int low) => new(Functions.BitVec.Extract(GetWidth(value.Sort), high, low), [value]);

        public static Term.FunctionApplication RotateLeft(Term value, int rotation) => new(Functions.BitVec.RotateLeft(GetWidth(value.Sort), rotation), [value]);

        public static Term.FunctionApplication RotateRight(Term value, int rotation) => new(Functions.BitVec.RotateRight(GetWidth(value.Sort), rotation), [value]);

        public static Term.FunctionApplication Repeat(Term value, int count) => new(Functions.BitVec.Repeat(GetWidth(value.Sort), count), [value]);

        public static Term.FunctionApplication UnsignedLessOrEqual(Term left, Term right) => new(Functions.BitVec.UnsignedLessOrEqual(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication SignedLessOrEqual(Term left, Term right) => new(Functions.BitVec.SignedLessOrEqual(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication UnsignedLessThan(Term left, Term right) => new(Functions.BitVec.UnsignedLessThan(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication SignedLessThan(Term left, Term right) => new(Functions.BitVec.SignedLessThan(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication UnsignedGreaterOrEqual(Term left, Term right) => new(Functions.BitVec.UnsignedGreaterOrEqual(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication SignedGreaterOrEqual(Term left, Term right) => new(Functions.BitVec.SignedGreaterOrEqual(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication UnsignedGreaterThan(Term left, Term right) => new(Functions.BitVec.UnsignedGreaterThan(GetWidth(left.Sort)), [left, right]);

        public static Term.FunctionApplication SignedGreaterThan(Term left, Term right) => new(Functions.BitVec.SignedGreaterThan(GetWidth(left.Sort)), [left, right]);

        private static int GetWidth(Sort sort)
        {
            if (sort is not Sort.BitVec bitVec)
            {
                throw new ArgumentException("Expected bit-vector sort", nameof(sort));
            }

            return bitVec.Width;
        }
    }
}

