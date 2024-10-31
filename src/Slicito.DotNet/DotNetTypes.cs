using Slicito.Abstractions;
using Slicito.Abstractions.Queries;

namespace Slicito.DotNet;

public class DotNetTypes(ITypeSystem typeSystem)
{
    public ElementType Project { get; } = typeSystem.GetElementType([("Kind", "Project")]);
}
