using System.Collections.Immutable;

namespace Slicito.Presentation;

public record PageNavigationOptions(GetUriDelegate? GetUriDelegate);

public record DynamicPageNavigationOptions(
    GetUriDelegate? GetUriDelegate,
    IImmutableDictionary<string, string> Parameters) : PageNavigationOptions(GetUriDelegate);
