using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

internal class MethodDependencyRelationsWalker : CSharpSyntaxWalker
{
    private readonly DotNetContext _context;
    private readonly DependencyRelations.Builder _builder;

    private readonly DotNetMethod _method;
    private readonly SemanticModel _semanticModel;

    public MethodDependencyRelationsWalker(
        DotNetContext context,
        DependencyRelations.Builder builder,
        DotNetMethod method,
        SemanticModel semanticModel)
    {
        _context = context;
        _builder = builder;
        _method = method;
        _semanticModel = semanticModel;
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        base.VisitInvocationExpression(node);

        if (_semanticModel.GetSymbolInfo(node).Symbol is not IMethodSymbol calleeSymbol)
        {
            return;
        }

        if (_context.TryGetElementFromSymbol(calleeSymbol) is not DotNetMethod calleeElement)
        {
            return;
        }

        _builder.Calls.Add(_method, calleeElement, node);
    }
}
