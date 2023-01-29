using System.Diagnostics;
using System.Text.Encodings.Web;

using Slicito.Presentation;

namespace SolutionAnalysis;

internal static class Utils
{
    public static void SaveSvgAndOpen(Schema schema)
    {
        var svgPath = Path.GetFullPath("schema.svg");
        using (var writer = new StreamWriter(svgPath))
        {
            schema.WriteHtmlTo(writer);
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
}
