namespace Slicito.ProgramAnalysis.Strings;

public abstract record CharacterClass
{
    private CharacterClass() { }
    
    public sealed record Any : CharacterClass;

    public sealed record Single(char Value) : CharacterClass;

    public sealed record Range(char From, char To) : CharacterClass;

    public sealed record Union(CharacterClass Left, CharacterClass Right) : CharacterClass;
}
