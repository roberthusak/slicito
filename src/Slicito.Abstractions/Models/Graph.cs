using System.Collections.Immutable;

namespace Slicito.Abstractions.Models;

public sealed record Graph(ImmutableArray<Node> Nodes, ImmutableArray<Edge> Edges) : IModel;

public sealed record Node(string Id, string? Label, Command? ClickCommand);

public sealed record Edge(string SourceId, string TargetId, string? Label, Command? ClickCommand);
