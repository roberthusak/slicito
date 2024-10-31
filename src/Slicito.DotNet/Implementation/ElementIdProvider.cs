using Microsoft.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;

internal static class ElementIdProvider
{
    public static ElementId GetElementId(Project project) => new(project.FilePath!);
}
