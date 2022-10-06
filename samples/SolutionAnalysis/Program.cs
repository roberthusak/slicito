using Microsoft.CodeAnalysis;
using Microsoft.Msagl.Drawing;
using Slicito;
using SolutionAnalysis;

var project = await RoslynUtils.OpenProjectAsync(args[0]);
var compilation = await project.GetCompilationAsync();

var graph = await CreateGraphFromSymbolsAsync(compilation!.GetSymbolsWithName(_ => true, SymbolFilter.Namespace | SymbolFilter.Type));

var uri = await graph.RenderToSvgUriAsync();
Utils.OpenUri(uri);


async Task<Graph> CreateGraphFromSymbolsAsync(IEnumerable<ISymbol> symbols, ISymbol? topLevelSymbol = null)
{
    var graph = new Graph();

    if (topLevelSymbol is not null)
    {
        graph.AddSymbol(topLevelSymbol);
    }

    foreach (var symbol in symbols)
    {
        if (symbol is INamespaceSymbol { IsGlobalNamespace: true })
        {
            continue;
        }

        var symbolSubgraph = graph.AddSymbolWithHierarchy(symbol);

        if (symbol is ITypeSymbol typeSymbol)
        {
            var detailGraph = await CreateGraphFromSymbolsAsync(typeSymbol.GetMembers(), typeSymbol);

            symbolSubgraph.Attr.Uri = await detailGraph.RenderToSvgUriAsync(filename: $"schema_{typeSymbol.GetNodeId()}.svg");
        }

        if (symbol is IMethodSymbol methodSymbol)
        {
            AddEdgesToCallees(graph, symbolSubgraph, methodSymbol, topLevelSymbol);
        }
    }

    return graph;
}

void AddEdgesToCallees(Graph graph, Subgraph callerSubgraph, IMethodSymbol callerSymbol, ISymbol? topLevelSymbol = null)
{
    foreach (var invocation in callerSymbol.FindCallees(compilation))
    {
        Node edgeFrom;
        Node edgeTo;

        if (topLevelSymbol is not null && !SymbolEqualityComparer.Default.Equals(invocation.Callee.ContainingType, topLevelSymbol))
        {
            // Reference to an outside dependency, display only the edge from the current type to its type
            edgeFrom = graph.AddSymbol(topLevelSymbol);
            edgeTo = graph.AddSymbolWithHierarchy(invocation.Callee.ContainingType);
        }
        else
        {
            edgeFrom = callerSubgraph;
            edgeTo = graph.AddSymbolWithHierarchy(invocation.Callee);
        }

        var edge = edgeFrom.Edges.FirstOrDefault(edge => edge.Target == edgeTo.Id);
        if (edge is not null)
        {
            // Only increase the thickness (up to a maximum) but not add another edge between the two
            edge.Attr.LineWidth = Math.Min(5.0, edge.Attr.LineWidth * 1.2);

            continue;
        }

        edge = graph.AddEdge(edgeFrom.Id, edgeTo.Id);

        var callSite = invocation.CallSite;
        var position = callSite.SyntaxTree.GetMappedLineSpan(callSite.Span);

        edge.Attr.Uri = ServerUtils.GetOpenFileEndpointUri(position);
    }
}
