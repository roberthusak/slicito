using Slicito.Abstractions;

namespace Slicito.DotNet.AspNetCore;

public record ApiEndpoint(HttpMethod Method, string Path, ElementInfo HandlerElement);
