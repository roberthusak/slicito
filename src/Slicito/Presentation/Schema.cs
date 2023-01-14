using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Html;
using Microsoft.Msagl.Drawing;

namespace Slicito.Presentation;

public partial class Schema : IContent, IHtmlContent
{
    private readonly Graph _graph;

    private Schema(Graph graph)
    {
        _graph = graph;
    }

    public void WriteHtmlTo(TextWriter writer) => _graph.RenderSvgToTextWriter(writer, LayoutOrientation.Vertical, false);

    public void WriteMarkdownTo(TextWriter writer) => WriteHtmlTo(writer);

    public void WriteTo(TextWriter writer, HtmlEncoder encoder) => WriteHtmlTo(writer);
}
