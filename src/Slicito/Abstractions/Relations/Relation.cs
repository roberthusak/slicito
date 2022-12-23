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
        Merge<TSourceElement, TTargetElement, TData>(
            params IBinaryRelation<TSourceElement, TTargetElement, TData>[] relations)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    =>
        Merge((IEnumerable<IBinaryRelation<TSourceElement, TTargetElement, TData>>) relations);

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
        Transform<TSourceElement, TTargetElement, TDataFrom, TDataTo>(
            this IBinaryRelation<TSourceElement, TTargetElement, TDataFrom> relation,
            Func<IPair<TSourceElement, TTargetElement, TDataFrom>, IPair<TSourceElement, TTargetElement, TDataTo>> transformer)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    =>
        new BinaryRelation<TSourceElement, TTargetElement, TDataTo>.Builder()
        .AddRange(
            relation.Pairs.Select(pair => transformer(pair)))
        .Build();

    public static IBinaryRelation<TSourceElement, TTargetElement, TDataTo>
        TransformData<TSourceElement, TTargetElement, TDataFrom, TDataTo>(
            this IBinaryRelation<TSourceElement, TTargetElement, TDataFrom> relation,
            Func<TDataFrom, TDataTo> dataTransformer)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    =>
        relation.Transform(pair => Pair.Create(pair.Source, pair.Target, dataTransformer(pair.Data)));

    public static IBinaryRelation<TSourceElement, TTargetElement, TDataTo>
        SetData<TSourceElement, TTargetElement, TDataFrom, TDataTo>(
            this IBinaryRelation<TSourceElement, TTargetElement, TDataFrom> relation,
            TDataTo dataValue)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    =>
        relation.Transform(pair => Pair.Create(pair.Source, pair.Target, dataValue));

    public static IBinaryRelation<TTargetElement, TSourceElement, TData>
        Invert<TSourceElement, TTargetElement, TData>(
            this IBinaryRelation<TSourceElement, TTargetElement, TData> relation)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    =>
        new BinaryRelation<TTargetElement, TSourceElement, TData>.Builder()
        .AddRange(
            relation.Pairs.Select(pair => Pair.Create(pair.Target, pair.Source, pair.Data)))
        .Build();

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

    public static IBinaryRelation<TElement, TElement, TData>
        CompactPaths<TElement, TData>(
            this IBinaryRelation<TElement, TElement, TData> relation,
            Predicate<IPair<TElement, TElement, TData>>? pairFilter = null,
            Predicate<IEnumerable<IPair<TElement, TElement, TData>>>? pathFilter = null,
            Func<IEnumerable<IPair<TElement, TElement, TData>>, TData>? dataProvider = null)
        where TElement : class, IElement
    {
        var builder = new BinaryRelation<TElement, TElement, TData>.Builder();

        var processedCompactablePairs = new HashSet<IPair<TElement, TElement, TData>>();

        foreach (var pair in relation.Pairs)
        {
            if (pair.Source == pair.Target)
            {
                // Don't remove loops
                builder.Add(pair);
                continue;
            }

            if (processedCompactablePairs.Contains(pair))
            {
                continue;
            }

            if (!IsReducibleElement(pair.Source) && !IsReducibleElement(pair.Target))
            {
                builder.Add(pair);
                continue;
            }

            var path = new List<IPair<TElement, TElement, TData>> { pair };

            ExtendPathStart(path);
            ExtendPathEnd(path);

            processedCompactablePairs.UnionWith(path);

            if (path.Count == 1
                || !(pathFilter?.Invoke(path) ?? true))
            {
                builder.AddRange(path);
                continue;
            }

            var data = dataProvider is not null
                ? dataProvider(path)
                : path.First().Data;

            builder.Add(path.First().Source, path.Last().Target, data);
        }

        return builder.Build();

        void ExtendPathStart(List<IPair<TElement, TElement, TData>> path)
        {
            for (var pair = path.First(); IsReducibleElement(pair.Source);)
            {
                pair = relation.GetIncoming(pair.Source).Single();

                if (pair.Source == path.Last().Target           // Don't compact cycles
                    || !ProcessPathExtensionPair(path, pair))
                {

                    break;
                }

                path.Insert(0, pair);
            }
        }

        void ExtendPathEnd(List<IPair<TElement, TElement, TData>> path)
        {
            for (var pair = path.Last(); IsReducibleElement(pair.Target);)
            {
                pair = relation.GetOutgoing(pair.Target).Single();

                if (pair.Target == path.First().Source          // Don't compact cycles
                    || !ProcessPathExtensionPair(path, pair))
                {
                    break;
                }

                path.Add(pair);
            }
        }

        bool ProcessPathExtensionPair(List<IPair<TElement, TElement, TData>> path, IPair<TElement, TElement, TData> pair)
        {
            if (processedCompactablePairs.Contains(pair))
            {
                return false;
            }

            if (!(pairFilter?.Invoke(pair) ?? true))
            {
                // Ensure that the filter is called on each pair at most once
                builder.Add(pair);
                processedCompactablePairs.Add(pair);
                return false;
            }

            return true;
        }

        bool IsReducibleElement(TElement element) =>
            relation.GetIncoming(element).Count() == 1
            && relation.GetOutgoing(element).Count() == 1;
    }

    public static bool Contains<TElement, TData>(
        this IBinaryRelation<TElement, TElement, TData> relation,
        TElement element)
    where TElement : class, IElement
    =>
        relation.Pairs.Any(pair => element == pair.Source || element == pair.Target);

    public static ISet<TElement> GetElements<TElement, TData>(
        this IBinaryRelation<TElement, TElement, TData> relation)
    where TElement : class, IElement
    {
        var set = new HashSet<TElement>(relation.Sources);
        set.UnionWith(relation.Targets);

        return set;
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

    /// <remarks>
    /// Works only on hierarchies, i.e. relations where each element is a target of at most one pair.
    /// </remarks>
    public static IEnumerable<TElement> GetAncestorsOrSelf<TElement, TData>(
        this IBinaryRelation<TElement, TElement, TData> hierarchy,
        TElement element)
    where TElement : class, IElement
    {
        yield return element;

        foreach (var ancestor in hierarchy.GetAncestors(element))
        {
            yield return ancestor;
        }
    }

    public static IBinaryRelation<TElement, TElement, TData> SliceForward<TElement, TData>(
        this IBinaryRelation<TElement, TElement, TData> relation,
        IEnumerable<TElement> sourceElements)
    where TElement : class, IElement
    {
        var builder = new BinaryRelation<TElement, TElement, TData>.Builder();

        var stack = new Stack<TElement>();
        foreach (var element in sourceElements)
        {
            stack.Push(element);
        }

        var visited = new HashSet<TElement>();

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (!visited.Add(current))
            {
                continue;
            }

            foreach (var pair in relation.GetOutgoing(current))
            {
                builder.Add(pair);
                stack.Push(pair.Target);
            }
        }

        return builder.Build();
    }

    public static IBinaryRelation<TElement, TElement, TData>
        SliceForward<TElement, TData>(
            this IBinaryRelation<TElement, TElement, TData> relation,
            TElement sourceElement)
        where TElement : class, IElement
    =>
        relation.SliceForward(new[] { sourceElement });

    public static IBinaryRelation<TElement, TElement, TData>
        CreateTransitiveClosure<TElement, TData>(
            this IBinaryRelation<TElement, TElement, TData> relation)
        where TElement : class, IElement
    {
        var builder = new BinaryRelation<TElement, TElement, TData>.Builder();

        foreach (var sourceElement in relation.Sources)
        {
            foreach (var pair in relation.SliceForward(sourceElement).Pairs)
            {
                builder.Add(sourceElement, pair.Target, pair.Data);
            }
        }

        return builder.Build();
    }

    public static IBinaryRelation<TSourceElement, TTargetElement, TData>
        Join<TSourceElement, TInnerElement, TTargetElement, TData>(
            this IBinaryRelation<TSourceElement, TInnerElement, TData> relation,
            IBinaryRelation<TInnerElement, TTargetElement, TData> joinedRelation)
        where TSourceElement : class, IElement
        where TInnerElement : class, IElement
        where TTargetElement : class, IElement
    {
        var builder = new BinaryRelation<TSourceElement, TTargetElement, TData>.Builder();

        foreach (var pairLeft in relation.Pairs)
        {
            foreach (var pairRight in joinedRelation.GetOutgoing(pairLeft.Target))
            {
                builder.Add(pairLeft.Source, pairRight.Target, pairRight.Data);
            }
        }

        return builder.Build();
    }
}
