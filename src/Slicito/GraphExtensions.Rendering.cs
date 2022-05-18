using Microsoft.AspNetCore.Html;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using System.Drawing;
using System.Web;

using Cluster = Microsoft.Msagl.Core.Layout.Cluster;
using LabelPlacement = Microsoft.Msagl.Core.Layout.LgNodeInfo.LabelPlacement;

namespace Slicito;

public static partial class GraphExtensions
{
    public static IHtmlContent RenderToSvg(this Graph graph)
    {
        EnsureLayout(graph);

        using var ms = new MemoryStream();
        var svgWriter = new CustomSvgGraphWriter(ms, graph);
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

        foreach (var subgraph in graph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf())
        {
            if (subgraph.Label.Text == null)
            {
                subgraph.Label.Text = subgraph.Id;
            }

            EnsureLabelDimensions(subgraph.Label);
            EnsureSubgraphBoundary(subgraph);
        }

        foreach (var node in graph.Nodes)
        {
            if (node.Label.Text == null)
            {
                node.Label.Text = node.Id;
            }

            EnsureLabelDimensions(node.Label);
            EnsureNodeBoundary(node);
        }

        foreach (var edge in graph.Edges)
        {
            if (edge.Label != null)
            {
                EnsureLabelDimensions(edge.Label);
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

        // The text may contain escaped characters, e.g. '<' and '>'
        var text = HttpUtility.HtmlDecode(label.Text);

        var measurements = graphics.MeasureString(text, font);

        label.Width = measurements.Width;
        label.Height = measurements.Height;
    }

    private static void EnsureSubgraphBoundary(Subgraph subgraph)
    {
        var cluster = (Cluster)subgraph.GeometryNode;
        if (cluster.RectangularBoundary == null)
        {
            var labelPlacement = subgraph.Attr.ClusterLabelMargin;
            cluster.RectangularBoundary = new RectangularClusterBoundary
            {
                BottomMargin =
                    labelPlacement == LabelPlacement.Bottom
                    ? subgraph.Label.Height + subgraph.Attr.LabelMargin + subgraph.Attr.Padding
                    : 0,
                LeftMargin =
                    labelPlacement == LabelPlacement.Left
                    ? subgraph.Label.Width + subgraph.Attr.LabelMargin + subgraph.Attr.Padding
                    : 0,
                RightMargin =
                    labelPlacement == LabelPlacement.Right
                    ? subgraph.Label.Width + subgraph.Attr.LabelMargin + subgraph.Attr.Padding
                    : 0,
                TopMargin =
                    labelPlacement == LabelPlacement.Top
                    ? subgraph.Label.Height + subgraph.Attr.LabelMargin + subgraph.Attr.Padding
                    : 0,
                MinWidth = subgraph.Label.Width,
                MinHeight = subgraph.Label.Height
            };
        }
    }

    private static void EnsureNodeBoundary(Node node)
    {
        if (node.GeometryNode.BoundaryCurve == null)
        {
            node.GeometryNode.BoundaryCurve = NodeBoundaryCurves.GetNodeBoundaryCurve(
                node,
                node.Label.Width + 2 * node.Attr.LabelMargin + 2 * node.Attr.Padding,
                node.Label.Height + 2 * node.Attr.LabelMargin + 2 * node.Attr.Padding);
        }
    }
}
