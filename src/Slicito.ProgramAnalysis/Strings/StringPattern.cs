namespace Slicito.ProgramAnalysis.Strings;

public abstract record StringPattern
{
    private StringPattern() { }

    public sealed record All : StringPattern
    {
        public static All Instance { get; } = new();
    }

    public sealed record Literal(string Value) : StringPattern;

    public sealed record Character(CharacterClass CharacterClass) : StringPattern;

    public sealed record Concatenation(StringPattern Left, StringPattern Right) : StringPattern;

    public sealed record Alternation(StringPattern Left, StringPattern Right) : StringPattern;

    public sealed record Loop(StringPattern Pattern, int Min, int? Max) : StringPattern;
}
