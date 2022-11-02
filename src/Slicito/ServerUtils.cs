using Microsoft.AspNetCore.WebUtilities;
using Microsoft.CodeAnalysis;

namespace Slicito;

public static class ServerUtils
{
    public const string BaseUri = "https://localhost:7032";

    public static Uri GetOpenFileEndpointUri(FileLinePositionSpan position)
    {
        // Both line and character offset usually start at 1 in IDEs
        var line = position.Span.Start.Line + 1;
        var offset = position.Span.Start.Character + 1;

        var query = new Dictionary<string, string>()
        {
            { "path", position.Path },
            { "line", line.ToString() },
            { "offset", offset.ToString() }
        };

        return new(QueryHelpers.AddQueryString($"{BaseUri}/open", query));
    }

    public static async Task<string> UploadFileAsync(string filename, Stream content)
    {
        var fileUri = $"{BaseUri}/files/{Uri.EscapeDataString(filename)}";

        using var httpClient = new HttpClient();

        HttpResponseMessage response;

        try
        {
            response = await httpClient.PutAsync(fileUri, new StreamContent(content));
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(GetFileUploadError(filename), e);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(GetFileUploadError(filename));
        }

        return fileUri;
    }

    private static string GetFileUploadError(string filename) =>
        $"Unable to upload file '{filename}' to the Slicito server. " +
        $"Please make sure the server is running and available at the following URI: {BaseUri}";
}
