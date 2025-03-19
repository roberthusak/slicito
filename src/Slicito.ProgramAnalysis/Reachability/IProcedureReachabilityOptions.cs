using Slicito.ProgramAnalysis.Notation.TypedExpressions;

namespace Slicito.ProgramAnalysis.Reachability;

public interface IProcedureReachabilityOptions
{
    BooleanExpression GetBooleanParameter(string name);

    IntegerExpression GetIntegerParameter(string name);

    StringExpression GetStringParameter(string name);

    BooleanExpression GetBooleanReturnValue();

    IntegerExpression GetIntegerReturnValue();

    StringExpression GetStringReturnValue();

    IProcedureReachabilityOptions AddConstraint(BooleanExpression constraint);
}
