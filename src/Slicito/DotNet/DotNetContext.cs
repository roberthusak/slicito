using System.Collections.Immutable;

using Slicito.Abstractions;
using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;
using Slicito.DotNet.Relations;

namespace Slicito.DotNet;

public class DotNetContext : IContext<DotNetElement, EmptyStruct>
{
    public ISet<DotNetElement> Elements => throw new NotImplementedException();

    public IBinaryRelation<DotNetElement, DotNetElement, EmptyStruct> Hierarchy => throw new NotImplementedException();

    private DotNetContext(ImmutableArray<DotNetElement> elements, HierarchyRelation hierarchy)
    {
        throw new NotImplementedException();
    }

    public InterproceduralRelations ExtractInterproceduralRelations(Predicate<DotNetElement>? filter = null)
    {
        throw new NotImplementedException();
    }



    private class HierarchyRelation : IBinaryRelation<DotNetElement, DotNetElement, EmptyStruct>
    {
        public IEnumerable<IPair<DotNetElement, DotNetElement, EmptyStruct>> Pairs => throw new NotImplementedException();
    }

    public class Builder
    {
        public Builder AddProject(string projectPath)
        {
            throw new NotImplementedException();
        }

        public DotNetContext Build()
        {
            throw new NotImplementedException();
        }
    }
}
