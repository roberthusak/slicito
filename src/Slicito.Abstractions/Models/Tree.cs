using System.Collections.Immutable;

namespace Slicito.Abstractions.Models;

public sealed record Tree(ImmutableArray<TreeItem> Items) : IModel;

public sealed record TreeItem(string Name, ImmutableArray<TreeItem> Children, Command? DoubleClickCommand = null);
