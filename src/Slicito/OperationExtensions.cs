using Microsoft.CodeAnalysis;

namespace Slicito;

public static class OperationExtensions
{
    public static Uri GetFileOpenUri(this IOperation operation)
    {
        var location = operation.Syntax.GetLocation();

        return ServerUtils.GetOpenFileEndpointUri(location.GetMappedLineSpan());
    }
}
