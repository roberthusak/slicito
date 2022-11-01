using System.Collections;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public class InterproceduralRelations : IEnumerable<IBinaryRelation<DotNetElement, DotNetElement, SyntaxNode?>>
{
    public IBinaryRelation<DotNetMethod, DotNetMethod, SyntaxNode> Calls { get; }

    public IBinaryRelation<DotNetMethod, DotNetStorageMember, SyntaxNode> Stores { get; }

    public IBinaryRelation<DotNetMethod, DotNetStorageMember, SyntaxNode> Loads { get; }

    public IBinaryRelation<DotNetElement, DotNetType, SyntaxNode> ReferencesType { get; }

    public IEnumerator<IBinaryRelation<DotNetElement, DotNetElement, SyntaxNode?>> GetEnumerator()
    {
        yield return Calls;
        yield return Stores;
        yield return Loads;
        yield return ReferencesType;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
