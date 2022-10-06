using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Slicito.Roslyn;

internal class InvocationWalker : CSharpSyntaxWalker
{
    private readonly IMethodSymbol _caller;
    private readonly SemanticModel _semanticModel;

    List<InvocationInfo>? _invocations;

    public static IEnumerable<InvocationInfo> FindInvocations(
        IMethodSymbol callerSymbol,
        BaseMethodDeclarationSyntax callerDeclaration,
        SemanticModel semanticModel)
    {
        var walker = new InvocationWalker(callerSymbol, semanticModel);
        walker.Visit(callerDeclaration);

        return walker._invocations ?? Enumerable.Empty<InvocationInfo>();
    }

    private InvocationWalker(IMethodSymbol caller, SemanticModel semanticModel)
    {
        _caller = caller;
        _semanticModel = semanticModel;
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        base.VisitInvocationExpression(node);

        if (_semanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol calleeSymbol)
        {
            return;
        }

        _invocations ??= new();
        _invocations.Add(new(_caller, calleeSymbol, node));
    }
}
