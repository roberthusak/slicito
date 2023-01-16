using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Slicito.Presentation;

namespace Slicito.Interactive;

public static class IdeUtils
{
    public const string OpenInIdePageId = "openInIde";

    public static PageNavigationDestination GetOpenInIdePageNavigationDestination(FileLinePositionSpan location)
    {
        // Both line and character offset usually start at 1 in IDEs
        var line = location.Span.Start.Line + 1;
        var offset = location.Span.Start.Character + 1;

        var paramaters = new Dictionary<string, string>()
        {
            { "path", location.Path },
            { "line", line.ToString() },
            { "offset", offset.ToString() }
        };

        return new(OpenInIdePageId, paramaters.ToImmutableDictionary());
    }

    public static Site.Builder AddOpenInIdePage(this Site.Builder builder, InteractiveSession? session = null)
    {
        builder.AddDynamicPage(OpenInIdePageId, options =>
        {
            var path = options.Parameters["path"];
            var line = int.Parse(options.Parameters["line"]);
            var offset = int.Parse(options.Parameters["offset"]);

            (session ?? InteractiveSession.Global).OpenFileInIde(path, line, offset);

            return null;
        });

        return builder;
    }
}
