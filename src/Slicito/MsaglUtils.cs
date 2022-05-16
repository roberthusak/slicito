using Microsoft.AspNetCore.Html;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using System.Diagnostics;

namespace Slicito;

public static class MsaglUtils
{
    public static IHtmlContent RenderSvg(Graph graph)
    {
        if (graph.GeometryGraph == null)
        {
            CalculateDefaultLayout(graph);
        }

        using var ms = new MemoryStream();
        var svgWriter = new SvgGraphWriter(ms, graph);
        svgWriter.Write();

        ms.Position = 0;
        var reader = new StreamReader(ms);
        var svgString = reader.ReadToEnd();

        return new HtmlString(svgString);
    }

    private static void CalculateDefaultLayout(Graph graph)
    {
        Debug.Assert(graph.GeometryGraph == null);

        graph.CreateGeometryGraph();

        foreach (var n in graph.Nodes)
        {
            n.GeometryNode.BoundaryCurve = CurveFactory.CreateRectangleWithRoundedCorners(60, 40, 3, 2, new Point(0, 0));

            n.Label.Width = n.Width * 0.6;
            n.Label.Height = 40;
            n.Label.Text = n.Id;
        }

        foreach (var e in graph.Edges)
        {
            e.Label.GeometryLabel.Width = 140;
            e.Label.GeometryLabel.Height = 60;
        }

        LayoutHelpers.CalculateLayout(graph.GeometryGraph, new SugiyamaLayoutSettings(), null);
    }
}
