using Microsoft.CodeAnalysis;
using Microsoft.Msagl.Drawing;
using Slicito;
using SolutionAnalysis;

var project = await RoslynUtils.OpenProjectAsync(args[0]);
var compilation = await project.GetCompilationAsync();

var graph = new Graph("graph");

foreach (var symbol in compilation!.GetSymbolsWithName(_ => true, SymbolFilter.Namespace | SymbolFilter.Type))
{
    var subgraph = graph.AddSymbolWithHierarchy(symbol);
}

var svg = graph.RenderToStaticSvg(LayoutOrientation.Vertical);
Utils.SaveSvgAndOpen(svg);
