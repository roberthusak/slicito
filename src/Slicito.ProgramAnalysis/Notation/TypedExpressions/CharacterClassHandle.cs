using Slicito.ProgramAnalysis.Strings;

namespace Slicito.ProgramAnalysis.Notation.TypedExpressions;

public readonly struct CharacterClassHandle
{
    public CharacterClass CharacterClass { get; }

    public CharacterClassHandle(CharacterClass characterClass)
    {
        CharacterClass = characterClass;
    }

    public static implicit operator CharacterClassHandle(char character)
    {
        return new CharacterClassHandle(new CharacterClass.Single(character));
    }

    public static CharacterClassHandle operator |(CharacterClassHandle left, CharacterClassHandle right)
    {
        return new CharacterClassHandle(new CharacterClass.Union(left.CharacterClass, right.CharacterClass));
    }
    
}
