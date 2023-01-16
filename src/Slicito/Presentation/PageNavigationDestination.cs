using System.Collections.Immutable;

namespace Slicito.Presentation;

public record PageNavigationDestination(string pageId, IImmutableDictionary<string, string> Parameters);
