using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Slicito.DotNet.Implementation;

internal class LambdaFinder : OperationWalker
{
    private readonly List<IFlowAnonymousFunctionOperation> _lambdas = [];

    public static IEnumerable<IFlowAnonymousFunctionOperation> FindLambdas(ControlFlowGraph roslynCfg)
    {
        var finder = new LambdaFinder();

        foreach (var block in roslynCfg.Blocks)
        {
            foreach (var operation in block.Operations)
            {
                finder.Visit(operation);
            }
            
            finder.Visit(block.BranchValue);
        }

        return finder._lambdas;
    }

    public override void VisitFlowAnonymousFunction(IFlowAnonymousFunctionOperation operation)
    {
        _lambdas.Add(operation);
    }
}
