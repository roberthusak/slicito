using Microsoft.CodeAnalysis;
using Microsoft.Msagl.Drawing;

namespace SolutionAnalysis;

class SymbolGraphVisitor : SymbolVisitor
{
    public Graph Graph { get; } = new Graph("graph");

    public override void DefaultVisit(ISymbol symbol)
    {
        var node = Graph.AddNode(GetNodeIdFromSymbol(symbol));
        node.LabelText = GetLabelFromSymbol(symbol);

        if (symbol.ContainingSymbol != null)
        {
            Graph.AddEdge(
                GetNodeIdFromSymbol(symbol.ContainingSymbol),
                "",
                GetNodeIdFromSymbol(symbol));
        }
    }

    public override void VisitModule(IModuleSymbol symbol)
    {
        DefaultVisit(symbol);

        symbol.GlobalNamespace.Accept(this);
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        DefaultVisit(symbol);

        VisitAll(symbol.GetMembers());
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        DefaultVisit(symbol);

        VisitAll(symbol.GetMembers());
    }

    private void VisitAll(IEnumerable<ISymbol> symbols)
    {
        foreach (var symbol in symbols)
        {
            symbol.Accept(this);
        }
    }

    // FIXME
    private string GetNodeIdFromSymbol(ISymbol symbol) =>
        symbol.GetHashCode().ToString();

    private string GetLabelFromSymbol(ISymbol symbol) =>
        string.IsNullOrEmpty(symbol.MetadataName) ? symbol.GetType().Name : symbol.MetadataName;
}
