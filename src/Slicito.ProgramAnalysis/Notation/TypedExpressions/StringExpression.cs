namespace Slicito.ProgramAnalysis.Notation.TypedExpressions;

public readonly struct StringExpression
{
    public Expression Expression { get; }

    public StringExpression(Expression expression)
    {
        Expression = expression;

        if (expression.GetDataType() is not DataType.Utf16String)
        {
            throw new InvalidOperationException("The expression is not a string.");
        }
    }

    public static implicit operator StringExpression(string value)
    {
        return new StringExpression(new Expression.Constant.Utf16String(value));
    }

    public IntegerExpression Length =>
        new(new Expression.UnaryOperator(UnaryOperatorKind.StringLength, Expression));

    public BooleanExpression StartsWith(StringExpression prefix) =>
        new(new Expression.BinaryOperator(BinaryOperatorKind.StringStartsWith, Expression, prefix.Expression));

    public BooleanExpression StartsWith(Func<CharacterClassProvider, CharacterClassHandle> characterClassCallback)
    {
        var characterClass = characterClassCallback(CharacterClassProvider.Instance);

        var pattern = new Expression.Constant.StringPattern(
            new Strings.StringPattern.Sequence(
                new Strings.StringPattern.Character(characterClass.CharacterClass),
                Strings.StringPattern.All.Instance
            )
        );

        return new(new Expression.BinaryOperator(BinaryOperatorKind.StringMatchesPattern, Expression, pattern));
    }

    public BooleanExpression EndsWith(StringExpression suffix) =>
        new(new Expression.BinaryOperator(BinaryOperatorKind.StringEndsWith, Expression, suffix.Expression));

    public BooleanExpression EndsWith(Func<CharacterClassProvider, CharacterClassHandle> characterClassCallback)
    {
        var characterClass = characterClassCallback(CharacterClassProvider.Instance);

        var pattern = new Expression.Constant.StringPattern(
            new Strings.StringPattern.Sequence(
                Strings.StringPattern.All.Instance,
                new Strings.StringPattern.Character(characterClass.CharacterClass)
            )
        );

        return new(new Expression.BinaryOperator(BinaryOperatorKind.StringMatchesPattern, Expression, pattern));
    }

    public BooleanExpression ContainsOnly(Func<CharacterClassProvider, CharacterClassHandle> characterClassCallback)
    {
        var characterClass = characterClassCallback(CharacterClassProvider.Instance);

        var pattern = new Expression.Constant.StringPattern(
            new Strings.StringPattern.Loop(
                new Strings.StringPattern.Character(characterClass.CharacterClass),
                Min: 0,
                Max: null
            )
        );

        return new(new Expression.BinaryOperator(BinaryOperatorKind.StringMatchesPattern, Expression, pattern));
    }
}
