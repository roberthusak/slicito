using System.Collections.Immutable;

namespace Slicito.Presentation;

public record PageNavigationDestination(string PageId, IImmutableDictionary<string, string> Parameters);
