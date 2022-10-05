using Microsoft.AspNetCore.WebUtilities;

namespace Slicito;

public static class ServerUtils
{
    public const string BaseUri = "https://localhost:7032";

    public static string GetOpenFileEndpointUri(string filepath, int line, int offset)
    {
        var query = new Dictionary<string, string>()
        {
            { "path", filepath },
            { "line", line.ToString() },
            { "offset", offset.ToString() }
        };

        return QueryHelpers.AddQueryString($"{BaseUri}/open", query);
    }

    public static async Task<Uri> UploadFileAsync(string filename, Stream content)
    {
        var fileUri = new Uri($"{BaseUri}/files/{Uri.EscapeDataString(filename)}");

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
