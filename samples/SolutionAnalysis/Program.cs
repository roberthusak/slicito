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
    graph.AddSymbolWithHierarchy(symbol);
}

var svg = graph.RenderToSvg(LayoutOrientation.Vertical);
Utils.SaveSvgAndOpen(svg);
