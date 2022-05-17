using Microsoft.CodeAnalysis;
using Microsoft.Msagl.Drawing;
using Slicito;
using System.Diagnostics;
using System.Text.Encodings.Web;

var project = await RoslynUtils.OpenProjectAsync(@"C:\datamole\repos\personal\benchmarkdotnet-sample\BenchmarkDotNetSample.csproj");
var compilation = await project.GetCompilationAsync();

var graph = new Graph("graph");

// FIXME
string GetNodeIdFromSymbol(ISymbol symbol) =>
    symbol.GetHashCode().ToString();

string GetLabelFromSymbol(ISymbol symbol) =>
    string.IsNullOrEmpty(symbol.MetadataName) ? symbol.GetType().Name : symbol.MetadataName;

foreach (var symbol in compilation!.GetSymbolsWithName(_ => true, SymbolFilter.All))
{
    var node = graph.AddNode(GetNodeIdFromSymbol(symbol));
    node.LabelText = GetLabelFromSymbol(symbol);

    if (symbol.ContainingSymbol != null)
    {
        graph.AddEdge(
            GetNodeIdFromSymbol(symbol.ContainingSymbol),
            "",
            GetNodeIdFromSymbol(symbol));
    }
}

// Display SVG in the default application

var svg = MsaglUtils.RenderSvg(graph);

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
