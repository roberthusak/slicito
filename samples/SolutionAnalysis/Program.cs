using Microsoft.CodeAnalysis;
using Microsoft.Msagl.Drawing;
using Slicito;
using SolutionAnalysis;

var project = await RoslynUtils.OpenProjectAsync(args[0]);
var compilation = await project.GetCompilationAsync();

var graph = new Graph("graph");

foreach (var symbol in compilation!.GetSymbolsWithName(_ => true, SymbolFilter.All))
{
    graph.AddSymbolAsNode(symbol);

    if (symbol.ContainingSymbol != null)
    {
        graph.AddEdgeBetweenSymbols(symbol.ContainingSymbol, symbol);
    }
}

var svg = graph.RenderToSvg();
Utils.SaveSvgAndOpen(svg);
