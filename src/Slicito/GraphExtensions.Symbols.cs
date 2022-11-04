using Microsoft.CodeAnalysis;
using Microsoft.Msagl.Drawing;
using System.Diagnostics;

namespace Slicito;

public static partial class GraphExtensions
{
    public static Subgraph AddSymbol(this Graph graph, ISymbol symbol) =>
        graph.AddSymbol(symbol, graph.RootSubgraph);

    public static Subgraph AddSymbol(this Graph graph, ISymbol symbol, Subgraph containingSubgraph)
    {
        if (!graph.SubgraphMap.TryGetValue(symbol.GetUniqueNameWithinProject(), out var subgraph))
        {
            subgraph = new Subgraph(symbol.GetUniqueNameWithinProject())
            {
                LabelText = symbol.GetShortName()
            };

            subgraph.Attr.Uri = symbol.GetFileOpenUri()?.ToString();

            containingSubgraph.AddSubgraph(subgraph);

            // FIXME Hack to force SubgraphMap refresh
            var retrievedNode = graph.AddNode(subgraph.Id);
            Debug.Assert(ReferenceEquals(subgraph, retrievedNode));
        }

        return subgraph;
    }

    public static Subgraph AddSymbolWithHierarchy(this Graph graph, ISymbol symbol)
    {
        if (symbol.ContainingSymbol == null || symbol.ContainingSymbol is INamespaceSymbol { IsGlobalNamespace: true })
        {
            return graph.AddSymbol(symbol);
        }
        else
        {
            if (!graph.SubgraphMap.TryGetValue(symbol.ContainingSymbol.GetUniqueNameWithinProject(), out var containingSubgraph))
            {
                containingSubgraph = graph.AddSymbolWithHierarchy(symbol.ContainingSymbol);
            }

            return graph.AddSymbol(symbol, containingSubgraph);
        }
    }

    public static Edge AddEdgeBetweenSymbols(this Graph graph, ISymbol source, ISymbol target, string? label = null)
    {
        var edge = graph.AddEdge(source.GetUniqueNameWithinProject(), target.GetUniqueNameWithinProject());

        if (label != null)
        {
            edge.LabelText = label;
        }

        return edge;
    }
}
