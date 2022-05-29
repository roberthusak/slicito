using Microsoft.CodeAnalysis;
using System.Web;

namespace Slicito;

public static class SymbolExtensions
{
    public static string GetNodeId(this ISymbol symbol)
        => symbol.ToDisplayString();

    public static string GetNodeLabelText(this ISymbol symbol)
    {
        var label = symbol.Name;
        if (string.IsNullOrEmpty(label))
        {
            label = symbol.ToDisplayString();
        }

        // E.g. for "<global namespace>"
        return HttpUtility.HtmlEncode(label);
    }
}
