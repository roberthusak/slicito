using System.Diagnostics;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Html;

namespace Slicito.Presentation;

public class SchemaReference : IHtmlContent
{
    public Uri Uri { get; }

    internal SchemaReference(Uri uri)
    {
        Uri = uri;
    }

    public void Open()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo(Uri.ToString())
            {
                UseShellExecute = true
            }
        };
        process.Start();
    }

    public IHtmlContent ShowInIFrame(
        int height = 600)
    {
        return new HtmlString(
            $"<iframe src=\"{Uri}\" style=\"width: 100%;\" height=\"{height}\"></iframe>");
    }

    public void WriteTo(TextWriter writer, HtmlEncoder encoder) =>
        ShowInIFrame().WriteTo(writer, encoder);
}
