using Microsoft.CodeAnalysis;
using Microsoft.Msagl.Drawing;
using Slicito;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Web;

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

// Display SVG in the default application

var svg = graph.RenderToSvg();

var svgPath = Path.GetFullPath("schema.svg");
using (var writer = new StreamWriter(svgPath))
{
    svg.WriteTo(writer, HtmlEncoder.Default);
}

var process = new Process
{
    StartInfo = new ProcessStartInfo(svgPath)
    {
        UseShellExecute = true
    }
};
process.Start();
