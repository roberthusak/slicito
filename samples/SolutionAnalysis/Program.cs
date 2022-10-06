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
            AddEdgesToCallees(graph, symbolSubgraph, methodSymbol);
        }
    }

    return graph;
}

void AddEdgesToCallees(Graph graph, Subgraph callerSubgraph, IMethodSymbol callerSymbol)
{
    foreach (var invocation in callerSymbol.FindCallees(compilation))
    {
        var calleeSubgraph = graph.AddSymbolWithHierarchy(invocation.Callee);

        var edge = callerSubgraph.Edges.FirstOrDefault(edge => edge.Target == calleeSubgraph.Id);
        if (edge is not null)
        {
            // Only increase the thickness (up to a maximum) but not add another edge between the two
            edge.Attr.LineWidth = Math.Min(5.0, edge.Attr.LineWidth * 1.2);

            continue;
        }

        edge = graph.AddEdge(callerSubgraph.Id, calleeSubgraph.Id);

        var callSite = invocation.CallSite;
        var position = callSite.SyntaxTree.GetMappedLineSpan(callSite.Span);

        edge.Attr.Uri = ServerUtils.GetOpenFileEndpointUri(position);
    }
}
