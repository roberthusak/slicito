using Microsoft.CodeAnalysis;
using Microsoft.Msagl.Drawing;
using Slicito;
using SolutionAnalysis;

static async Task<Graph> CreateGraphFromSymbols(IEnumerable<ISymbol> symbols, ISymbol? topLevelSymbol = null)
{
    var graph = new Graph();

    if (topLevelSymbol is not null)
    {
        graph.AddSymbol(topLevelSymbol);
    }

    foreach (var symbol in symbols)
    {
        var subgraph = graph.AddSymbolWithHierarchy(symbol);

        if (symbol is ITypeSymbol typeSymbol)
        {
            var detailGraph = await CreateGraphFromSymbols(typeSymbol.GetMembers(), typeSymbol);

            subgraph.Attr.Uri = await detailGraph.RenderToSvgUriAsync(filename: $"schema_{typeSymbol.GetNodeId()}.svg");
        }
    }

    return graph;
}

var project = await RoslynUtils.OpenProjectAsync(args[0]);
var compilation = await project.GetCompilationAsync();

var graph = await CreateGraphFromSymbols(compilation!.GetSymbolsWithName(_ => true, SymbolFilter.Namespace | SymbolFilter.Type));

var uri = await graph.RenderToSvgUriAsync();
Utils.OpenUri(uri);
