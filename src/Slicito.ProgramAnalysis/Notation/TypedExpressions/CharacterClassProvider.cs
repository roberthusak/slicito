using Slicito.ProgramAnalysis.Strings;

namespace Slicito.ProgramAnalysis.Notation.TypedExpressions;

public class CharacterClassProvider
{
    public static CharacterClassProvider Instance { get; } = new();

    private CharacterClassProvider() { }

    public CharacterClassHandle Any { get; } = new(new CharacterClass.Any());

    public CharacterClassHandle Digit { get; } = new(NamedClasses.Digit);

    public CharacterClassHandle Lower { get; } = new(NamedClasses.Lower);

    public CharacterClassHandle Upper { get; } = new(NamedClasses.Upper);

    public CharacterClassHandle Letter { get; } = new(NamedClasses.Letter);

    public CharacterClassHandle Alphanumeric { get; } = new(NamedClasses.Alphanumeric);

    public CharacterClassHandle Character(char character) => new(new CharacterClass.Single(character));

    public CharacterClassHandle Range(char from, char to) => new(new CharacterClass.Range(from, to));
    
    private static class NamedClasses
    {
        public static CharacterClass.Range Digit { get; } = new('0', '9');

        public static CharacterClass.Range Lower { get; } = new('a', 'z');

        public static CharacterClass.Range Upper { get; } = new('A', 'Z');

        public static CharacterClass.Union Letter { get; } = new(Lower, Upper);

        public static CharacterClass.Union Alphanumeric { get; } = new(Digit, Letter);
    }
}
