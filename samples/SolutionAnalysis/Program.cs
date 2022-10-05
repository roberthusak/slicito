using Microsoft.CodeAnalysis;
using Microsoft.Msagl.Drawing;
using Slicito;
using SolutionAnalysis;

var project = await RoslynUtils.OpenProjectAsync(args[0]);
var compilation = await project.GetCompilationAsync();

var graph = new Graph();

foreach (var symbol in compilation!.GetSymbolsWithName(_ => true, SymbolFilter.Namespace | SymbolFilter.Type))
{
    var subgraph = graph.AddSymbolWithHierarchy(symbol);

    if (symbol is ITypeSymbol typeSymbol)
    {
        var detailGraph = new Graph();

        detailGraph.AddSymbol(typeSymbol);

        foreach (var memberSymbol in typeSymbol.GetMembers())
        {
            detailGraph.AddSymbolWithHierarchy(memberSymbol);
        }

        var detailUri = await detailGraph.RenderToSvgUriAsync(filename: $"schema_{symbol.GetNodeId()}.svg");
        subgraph.Attr.Uri = detailUri.ToString();
    }
}

var uri = await graph.RenderToSvgUriAsync();
Utils.OpenUri(uri);
