using Microsoft.CodeAnalysis;
using Microsoft.Msagl.Drawing;

namespace Slicito;

public static partial class GraphExtensions
{
    public static Node AddSymbolAsNode(this Graph graph, ISymbol symbol)
    {
        var node = graph.AddNode(symbol.GetNodeId());
        node.LabelText = symbol.GetNodeLabelText();

        return node;
    }

    public static Edge AddEdgeBetweenSymbols(this Graph graph, ISymbol source, ISymbol target, string? label = null)
    {
        var edge = graph.AddEdge(source.GetNodeId(), target.GetNodeId());

        if (label != null)
        {
            edge.LabelText = label;
        }

        return edge;
    }
}
