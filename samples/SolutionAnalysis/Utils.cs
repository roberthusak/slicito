using System.Diagnostics;
using System.Text.Encodings.Web;

namespace SolutionAnalysis;

internal static class Utils
{
    public static void SaveSvgAndOpen(Microsoft.AspNetCore.Html.IHtmlContent svg)
    {
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
    }

    public static void OpenUri(string uri)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo(uri)
            {
                UseShellExecute = true
            }
        };
        process.Start();
    }
}
