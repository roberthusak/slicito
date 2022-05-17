// See https://aka.ms/new-console-template for more information
using Slicito;
using SolutionAnalysis;
using System.Diagnostics;
using System.Text.Encodings.Web;

var project = await RoslynUtils.OpenProjectAsync(@"C:\datamole\repos\personal\benchmarkdotnet-sample\BenchmarkDotNetSample.csproj");
var compilation = await project.GetCompilationAsync();

var graphVisitor = new SymbolGraphVisitor();
graphVisitor.Visit(compilation!.SourceModule);

// Display SVG in the default application

var svg = MsaglUtils.RenderSvg(graphVisitor.Graph);

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
