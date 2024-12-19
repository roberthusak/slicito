using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis.Reachability;

public interface IProcedureReachabilityOptions
{
    Variable GetParameter(string name);

    Variable GetReturnValue();

    IProcedureReachabilityOptions AddConstraint(Expression constraint);
}
