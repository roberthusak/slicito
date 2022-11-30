using Microsoft.AspNetCore.Html;
using Microsoft.Msagl.Core.Geometry;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Routing;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using Microsoft.Msagl.Miscellaneous;
using System.Diagnostics;
using System.Drawing;
using System.Web;

using Cluster = Microsoft.Msagl.Core.Layout.Cluster;
using LabelPlacement = Microsoft.Msagl.Core.Layout.LgNodeInfo.LabelPlacement;

namespace Slicito;

public enum LayoutOrientation
{
    Vertical,
    Horizontal
}

public static partial class GraphExtensions
{
    public static IHtmlContent RenderToStaticSvg(
        this Graph graph,
        LayoutOrientation orientation = LayoutOrientation.Vertical)
    {
        var ms = RenderSvgToStream(graph, orientation);

        var reader = new StreamReader(ms);
        var svgString = reader.ReadToEnd();

        return new HtmlString(svgString);
    }

    public static async Task<IHtmlContent> RenderToSvgAsync(
        this Graph graph,
        LayoutOrientation orientation = LayoutOrientation.Vertical,
        string filename = "schema.svg",
        int iframeWidth = 800,
        int iframeHeight = 600)
    {
        var fileUri = await RenderToSvgUriAsync(graph, orientation, filename);

        return new HtmlString(
            $"<iframe src=\"{fileUri}\" width=\"{iframeWidth}\" height=\"{iframeHeight}\"></iframe>");
    }

    public static async Task<Uri> RenderToSvgUriAsync(
        this Graph graph,
        LayoutOrientation orientation = LayoutOrientation.Vertical,
        string filename = "schema.svg")
    {
        var ms = RenderSvgToStream(graph, orientation);

        return await ServerUtils.UploadFileAsync(filename, ms);
    }

    public static MemoryStream RenderSvgToStream(this Graph graph, LayoutOrientation orientation)
    {
        EnsureLayout(graph, orientation);

        var ms = new MemoryStream();
        var svgWriter = new CustomSvgGraphWriter(ms, graph)
        {
            NodeSanitizer = (n => HttpUtility.HtmlEncode(n))
        };

        svgWriter.Write(Resources.SvgEmbeddedJavaScript);

        ms.Position = 0;
        return ms;
    }

    private static void EnsureLayout(Graph graph, LayoutOrientation orientation)
    {
        if (graph.GeometryGraph != null)
        {
            return;
        }

        // Set it before the geometry graph is generated since it can extract certain values from it (e.g. constraints)
        graph.LayoutAlgorithmSettings = new SugiyamaLayoutSettings
        {
            Transformation =
                orientation == LayoutOrientation.Horizontal
                ? PlaneTransformation.Rotation(Math.PI / 2)
                : PlaneTransformation.UnitTransformation,
            EdgeRoutingSettings = { EdgeRoutingMode = EdgeRoutingMode.Spline }
        };

        graph.CreateGeometryGraph();

        foreach (var subgraph in graph.RootSubgraph.AllSubgraphsDepthFirstExcludingSelf())
        {
            if (subgraph.Label.Text == null)
            {
                subgraph.Label.Text = subgraph.Id;
            }

            EnsureLabelDimensions(subgraph.Label);
            EnsureSubgraphBoundary(subgraph, orientation);
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

        LayoutHelpers.CalculateLayout(graph.GeometryGraph, graph.LayoutAlgorithmSettings, null);
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

    private static void EnsureSubgraphBoundary(Subgraph subgraph, LayoutOrientation orientation)
    {
        var cluster = (Cluster)subgraph.GeometryNode;
        if (cluster.RectangularBoundary == null)
        {
            var width = subgraph.Label.Width + subgraph.Attr.LabelMargin + subgraph.Attr.Padding;
            var height = subgraph.Label.Height + subgraph.Attr.LabelMargin + subgraph.Attr.Padding;
            var labelPlacement = subgraph.Attr.ClusterLabelMargin;

            if (orientation == LayoutOrientation.Vertical)
            {
                cluster.RectangularBoundary = new RectangularClusterBoundary
                {
                    BottomMargin = (labelPlacement == LabelPlacement.Bottom) ? height : 0,
                    LeftMargin = (labelPlacement == LabelPlacement.Left) ? width : 0,
                    RightMargin = (labelPlacement == LabelPlacement.Right) ? width : 0,
                    TopMargin = (labelPlacement == LabelPlacement.Top) ? height : 0,
                    MinWidth = width,
                    MinHeight = height
                };
            }
            else
            {
                Debug.Assert(orientation == LayoutOrientation.Horizontal);

                cluster.RectangularBoundary = new RectangularClusterBoundary
                {
                    BottomMargin = (labelPlacement == LabelPlacement.Right) ? width : 0,
                    LeftMargin = (labelPlacement == LabelPlacement.Bottom) ? height : 0,
                    RightMargin = (labelPlacement == LabelPlacement.Top) ? height : 0,
                    TopMargin = (labelPlacement == LabelPlacement.Left) ? width : 0,
                    MinWidth = height,
                    MinHeight = width
                };
            }
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
