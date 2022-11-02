namespace Slicito.Abstractions.Relations;

public static class Relation
{
    public static IBinaryRelation<TSourceElement, TTargetElement, TData>
        Merge<TSourceElement, TTargetElement, TData>(
            IEnumerable<IBinaryRelation<TSourceElement, TTargetElement, TData>> relations)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    {
        var builder = new BinaryRelation<TSourceElement, TTargetElement, TData>.Builder();

        foreach (var relation in relations)
        {
            builder.AddRange(relation.Pairs);
        }

        return builder.Build();
    }

    public static IBinaryRelation<TSourceElement, TTargetElement, TData>
        Filter<TSourceElement, TTargetElement, TData>(
            this IBinaryRelation<TSourceElement, TTargetElement, TData> relation,
            Predicate<IPair<TSourceElement, TTargetElement, TData>> filter)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    {
        var builder = new BinaryRelation<TSourceElement, TTargetElement, TData>.Builder();

        foreach (var pair in relation.Pairs)
        {
            if (filter(pair))
            {
                builder.Add(pair);
            }
        }

        return builder.Build();
    }

    public static IBinaryRelation<TSourceElement, TTargetElement, TData>
        MakeUnique<TSourceElement, TTargetElement, TData>(
            this IBinaryRelation<TSourceElement, TTargetElement, TData> relation)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    {
        var addedPairs = new HashSet<(string sourceId, string targetId)>();

        var builder = new BinaryRelation<TSourceElement, TTargetElement, TData>.Builder();

        foreach (var pair in relation.Pairs)
        {
            if (addedPairs.Add((pair.Source.Id, pair.Target.Id)))
            {
                builder.Add(pair);
            }
        }

        return builder.Build();
    }

    public static IBinaryRelation<TSourceElement, TTargetElement, TDataTo>
        TransformData<TSourceElement, TTargetElement, TDataFrom, TDataTo>(
            this IBinaryRelation<TSourceElement, TTargetElement, TDataFrom> relation,
            Func<TDataFrom, TDataTo> dataTransformer)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    {
        return
            new BinaryRelation<TSourceElement, TTargetElement, TDataTo>.Builder()
            .AddRange(
                relation.Pairs.Select(pair =>
                    Pair.Create(pair.Source, pair.Target, dataTransformer(pair.Data))))
            .Build();
    }

    public static IBinaryRelation<TElement, TElement, TData>
        MoveUpHierarchy<TElement, TData, THierarchyData>(
            this IBinaryRelation<TElement, TElement, TData> relation,
            IBinaryRelation<TElement, TElement, THierarchyData> hierarchy,
            Func<IPair<TElement, TElement, TData>, IPair<TElement, TElement, THierarchyData>, bool> moveFilter)
        where TElement : class, IElement
    =>
        relation.MoveUpHierarchy(hierarchy, moveFilter, moveFilter);

    public static IBinaryRelation<TElement, TElement, TData>
        MoveUpHierarchy<TElement, TData, THierarchyData>(
            this IBinaryRelation<TElement, TElement, TData> relation,
            IBinaryRelation<TElement, TElement, THierarchyData> hierarchy,
            Func<IPair<TElement, TElement, TData>, IPair<TElement, TElement, THierarchyData>, bool> sourceMoveFilter,
            Func<IPair<TElement, TElement, TData>, IPair<TElement, TElement, THierarchyData>, bool> targetMoveFilter)
        where TElement : class, IElement
    {
        var builder = new BinaryRelation<TElement, TElement, TData>.Builder();

        foreach (var pair in relation.Pairs)
        {
            var source = pair.Source;
            while (true)
            {
                var hierarchyEdge = hierarchy.GetIncoming(source).SingleOrDefault();
                if (hierarchyEdge is null || !sourceMoveFilter(pair, hierarchyEdge))
                {
                    break;
                }

                source = hierarchyEdge.Source;
            }

            var target = pair.Target;
            while (true)
            {
                var hierarchyEdge = hierarchy.GetIncoming(target).SingleOrDefault();
                if (hierarchyEdge is null || !targetMoveFilter(pair, hierarchyEdge))
                {
                    break;
                }

                target = hierarchyEdge.Source;
            }

            if (source == pair.Source && target == pair.Target)
            {
                builder.Add(pair);
            }
            else
            {
                builder.Add(Pair.Create(source, target, pair.Data));
            }
        }

        return builder.Build();
    }

    /// <remarks>
    /// Works only on hierarchies, i.e. relations where each element is a target of at most one pair.
    /// </remarks>
    public static IEnumerable<TElement> GetAncestors<TElement, TData>(
        this IBinaryRelation<TElement, TElement, TData> hierarchy,
        TElement element)
        where TElement : class, IElement
    {
        var current = element;
        while (true)
        {
            current = hierarchy.GetIncoming(current).SingleOrDefault()?.Source;
            if (current is null)
            {
                yield break;
            }

            yield return current;
        }
    }
}
