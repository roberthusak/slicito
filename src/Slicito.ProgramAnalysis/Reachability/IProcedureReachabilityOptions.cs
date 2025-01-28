using Slicito.ProgramAnalysis.Notation.TypedExpressions;

namespace Slicito.ProgramAnalysis.Reachability;

public interface IProcedureReachabilityOptions
{
    BooleanExpression GetBooleanParameter(string name);

    IntegerExpression GetIntegerParameter(string name);

    BooleanExpression GetBooleanReturnValue();

    IntegerExpression GetIntegerReturnValue();

    IProcedureReachabilityOptions AddConstraint(BooleanExpression constraint);
}
