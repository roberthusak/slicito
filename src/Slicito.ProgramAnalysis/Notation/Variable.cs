namespace Slicito.ProgramAnalysis.Notation;

public sealed class Variable(string name, DataType type)
{
    public string Name { get; } = name;
    public DataType Type { get; } = type;
}
