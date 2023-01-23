using System.Collections;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet.Relations;

public partial record DependencyRelations(
    IRelation<DotNetType, DotNetType, EmptyStruct> InheritsFrom,
    IRelation<DotNetMethod, DotNetMethod, EmptyStruct> Overrides,
    IRelation<DotNetOperation, DotNetMethod, SyntaxNode> Calls,
    IRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> StoresTo,
    IRelation<DotNetOperation, DotNetStorageTypeMember, SyntaxNode> LoadsFrom,
    IRelation<DotNetElement, DotNetType, SyntaxNode> ReferencesType,
    IRelation<DotNetStorageTypeMember, DotNetType, EmptyStruct> IsOfType)
    : IEnumerable<IRelation<DotNetElement, DotNetElement, SyntaxNode?>>
{
    public IEnumerator<IRelation<DotNetElement, DotNetElement, SyntaxNode?>> GetEnumerator()
    {
        yield return InheritsFrom.SetData((SyntaxNode?) null);
        yield return Overrides.SetData((SyntaxNode?) null);
        yield return Calls;
        yield return StoresTo;
        yield return LoadsFrom;
        yield return ReferencesType;
        yield return IsOfType.SetData((SyntaxNode?) null);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
