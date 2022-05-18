using Microsoft.CodeAnalysis;
using Microsoft.Msagl.Drawing;
using Slicito;
using SolutionAnalysis;
using System.Diagnostics;

var project = await RoslynUtils.OpenProjectAsync(args[0]);
var compilation = await project.GetCompilationAsync();

var graph = new Graph("graph");

foreach (var symbol in compilation!.GetSymbolsWithName(_ => true, SymbolFilter.Namespace | SymbolFilter.Type))
{
    AddSymbol(graph, symbol);
}

var svg = graph.RenderToSvg(LayoutOrientation.Vertical);
Utils.SaveSvgAndOpen(svg);


Subgraph AddSymbol(Graph graph, ISymbol symbol)
{
    if (symbol.ContainingSymbol == null)
    {
        return AddSubgraph(graph, graph.RootSubgraph, symbol);
    }
    else
    {
        if (!graph.SubgraphMap.TryGetValue(symbol.ContainingSymbol.GetNodeId(), out var containingSubgraph))
        {
            containingSubgraph = AddSymbol(graph, symbol.ContainingSymbol);
        }

        return AddSubgraph(graph, containingSubgraph, symbol);
    }
}

Subgraph AddSubgraph(Graph graph, Subgraph containingSubgraph, ISymbol symbol)
{
    if (!graph.SubgraphMap.TryGetValue(symbol.GetNodeId(), out var subgraph))
    {
        subgraph = new Subgraph(symbol.GetNodeId())
        {
            LabelText = symbol.GetNodeLabelText()
        };

        containingSubgraph.AddSubgraph(subgraph);

        // FIXME Hack to force SubgraphMap refresh
        var retrievedNode = graph.AddNode(subgraph.Id);
        Debug.Assert(ReferenceEquals(subgraph, retrievedNode));
    }

    return subgraph;
}
