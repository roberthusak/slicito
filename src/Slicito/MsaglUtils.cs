using Microsoft.AspNetCore.Html;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using System.Drawing;

namespace Slicito;

public static class MsaglUtils
{
    private const int Padding = 4;

    public static IHtmlContent RenderSvg(Graph graph)
    {
        EnsureLayout(graph);

        using var ms = new MemoryStream();
        var svgWriter = new SvgGraphWriter(ms, graph);
        svgWriter.Write();

        ms.Position = 0;
        var reader = new StreamReader(ms);
        var svgString = reader.ReadToEnd();

        return new HtmlString(svgString);
    }

    private static void EnsureLayout(Graph graph)
    {
        if (graph.GeometryGraph != null)
        {
            return;
        }

        graph.CreateGeometryGraph();

        foreach (var n in graph.Nodes)
        {
            if (n.Label.Text == null)
            {
                n.Label.Text = n.Id;
            }

            EnsureLabelDimensions(n.Label);

            if (n.GeometryNode.BoundaryCurve == null)
            {
                n.GeometryNode.BoundaryCurve = CurveFactory.CreateRectangleWithRoundedCorners(n.Label.Width + 2 * Padding, n.Label.Height + 2 * Padding, 3, 2, new (0, 0));
            }
        }

        foreach (var e in graph.Edges)
        {
            if (e.Label != null)
            {
                EnsureLabelDimensions(e.Label);
            }
        }

        var layoutSettings = new SugiyamaLayoutSettings
        {
            Transformation = PlaneTransformation.Rotation(Math.PI / 2),
            EdgeRoutingSettings = { EdgeRoutingMode = EdgeRoutingMode.Spline }
        };

        LayoutHelpers.CalculateLayout(graph.GeometryGraph, layoutSettings, null);
    }

    private static void EnsureLabelDimensions(Label label)
    {
        // TODO: Use cross-platform library (see https://stackoverflow.com/questions/69907690/using-c-sharp-to-measure-the-width-of-a-string-in-pixels-in-a-cross-platform-way)
        //       and take into account the DPI, SVG dimensions etc.

        using var bmp = new Bitmap(1, 1);
        using var graphics = Graphics.FromImage(bmp);

        var font = new Font(label.FontName, (float)label.FontSize);

        var measurements = graphics.MeasureString(label.Text, font);

        label.Width = measurements.Width;
        label.Height = measurements.Height;
    }
}
