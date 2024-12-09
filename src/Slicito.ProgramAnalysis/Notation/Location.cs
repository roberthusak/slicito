namespace Slicito.ProgramAnalysis.Notation;

public abstract class Location
{
    private Location() { }

    public sealed class VariableReference(Variable variable) : Location
    {
        public Variable Variable { get; } = variable;
    }
}
