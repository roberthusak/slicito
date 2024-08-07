using SlicitoGraph = Slicito.Abstractions.Models.Graph;
using MsaglGraph = Microsoft.Msagl.Drawing.Graph;

namespace Slicito.Wpf;

internal static class SlicitoToMsaglGraphConverter
{
    public static MsaglGraph Convert(SlicitoGraph slicitoGraph)
    {
        var msaglGraph = new MsaglGraph();

        foreach (var slicitoNode in slicitoGraph.Nodes)
        {
            var msaglNode = msaglGraph.AddNode(slicitoNode.Id);
            msaglNode.LabelText = slicitoNode.Label;
            msaglNode.UserData = slicitoNode;
        }

        foreach (var slicitoEdge in slicitoGraph.Edges)
        {
            var msaglEdge = msaglGraph.AddEdge(slicitoEdge.SourceId, slicitoEdge.Label ?? "", slicitoEdge.TargetId);
            msaglEdge.UserData = slicitoEdge;
        }

        return msaglGraph;
    }
}
