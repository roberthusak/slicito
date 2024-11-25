namespace Slicito.ProgramAnalysis.Notation;

public class Variable(string name, DataType type)
{
    public string Name { get; } = name;
    public DataType Type { get; } = type;
}
