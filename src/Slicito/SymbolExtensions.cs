using Microsoft.AspNetCore.WebUtilities;
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

    public static string? GetFileOpenUri(this ISymbol symbol)
    {
        var location = symbol.Locations.FirstOrDefault();
        if (location is null || !location.IsInSource)
        {
            return null;
        }

        var position = location.GetMappedLineSpan();

        // Both line and character offset usually start at 1 in IDEs
        var line = position.Span.Start.Line + 1;
        var offset = position.Span.Start.Character + 1;

        return ServerUtils.GetOpenFileEndpointUri(position.Path, line, offset);
    }
}
