using Microsoft.CodeAnalysis;
using Microsoft.Msagl.Drawing;

namespace SolutionAnalysis;

class SymbolGraphVisitor : SymbolVisitor
{
    public Graph Graph { get; } = new Graph("graph");

    private Stack<ISymbol> SymbolStack { get; } = new();

    public override void DefaultVisit(ISymbol symbol)
    {
        var node = Graph.AddNode(GetNodeIdFromSymbol(symbol));
        node.LabelText = GetLabelFromSymbol(symbol);

        if (SymbolStack.Count > 0)
        {
            Graph.AddEdge(
                GetNodeIdFromSymbol(SymbolStack.Peek()),
                "",
                GetNodeIdFromSymbol(symbol));
        }
    }

    public override void VisitModule(IModuleSymbol symbol)
    {
        DefaultVisit(symbol);

        SymbolStack.Push(symbol);
        symbol.GlobalNamespace.Accept(this);
        SymbolStack.Pop();
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        DefaultVisit(symbol);

        SymbolStack.Push(symbol);
        VisitAll(symbol.GetMembers());
        SymbolStack.Pop();
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        DefaultVisit(symbol);

        SymbolStack.Push(symbol);
        VisitAll(symbol.GetMembers());
        SymbolStack.Pop();
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
