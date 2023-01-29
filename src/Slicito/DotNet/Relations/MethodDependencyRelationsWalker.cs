using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

internal class MethodDependencyRelationsWalker : CSharpSyntaxWalker
{
    private readonly DotNetContext _context;
    private readonly DependencyRelations.Builder _builder;

    private readonly DotNetMethod _methodElement;
    private readonly SemanticModel _semanticModel;

    public MethodDependencyRelationsWalker(
        DotNetContext context,
        DependencyRelations.Builder builder,
        DotNetMethod methodElement,
        SemanticModel semanticModel)
    {
        _context = context;
        _builder = builder;
        _methodElement = methodElement;
        _semanticModel = semanticModel;
    }

    public override void VisitIdentifierName(IdentifierNameSyntax node)
    {
        base.VisitIdentifierName(node);

        var symbol = _semanticModel.GetSymbolInfo(node).Symbol;

        HandleTypeReference(node, symbol);
    }

    private void HandleTypeReference(SyntaxNode node, ISymbol? symbol)
    {
        if (symbol is not ITypeSymbol typeSymbol
            || _context.TryGetElementFromSymbol(typeSymbol) is not DotNetType typeElement)
        {
            return;
        }

        _builder.ReferencesType.Add(_methodElement, typeElement, node);
    }
}
