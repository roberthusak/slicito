using System.Collections.Immutable;

namespace Slicito.Abstractions.Models;

public record Command(string Name, ImmutableDictionary<string, string> Parameters);
