namespace Slicito.Abstractions.Relations;

public static class RelationExtensions
{
    public static IBinaryRelation<TSourceElement, TTargetElement, TData>
        Filter<TSourceElement, TTargetElement, TData>(
            this IBinaryRelation<TSourceElement, TTargetElement, TData> relation,
            Predicate<IPair<TSourceElement, TTargetElement, TData>> filter)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    {
        throw new NotImplementedException();
    }

    public static IBinaryRelation<TSourceElement, TTargetElement, TDataTo>
        TransformData<TSourceElement, TTargetElement, TDataFrom, TDataTo>(
            this IBinaryRelation<TSourceElement, TTargetElement, TDataFrom> relation,
            Func<TDataFrom, TDataTo> dataTransformer)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    {
        throw new NotImplementedException();
    }

    public static IBinaryRelation<TElement, TElement, TData>
        MoveUpHierarchy<TElement, TData, THierarchyData>(
            this IBinaryRelation<TElement, TElement, TData> relation,
            IBinaryRelation<TElement, TElement, THierarchyData> hierarchy,
            Func<IPair<TElement, TElement, TData>, IPair<TElement, TElement, THierarchyData>, bool> sourceMoveFilter,
            Func<IPair<TElement, TElement, TData>, IPair<TElement, TElement, THierarchyData>, bool> targetMoveFilter)
        where TElement : class, IElement
    {
        throw new NotImplementedException();
    }

    public static IBinaryRelation<TElement, TElement, TData>
        MoveUpHierarchy<TElement, TData, THierarchyData>(
            this IBinaryRelation<TElement, TElement, TData> relation,
            IBinaryRelation<TElement, TElement, THierarchyData> hierarchy,
            Func<IPair<TElement, TElement, TData>, IPair<TElement, TElement, THierarchyData>, bool> moveFilter)
        where TElement : class, IElement
    =>
        relation.MoveUpHierarchy(hierarchy, moveFilter, moveFilter);

    public static IBinaryRelation<TSourceElement, TTargetElement, TData>
        MakeUnique<TSourceElement, TTargetElement, TData>(
            this IBinaryRelation<TSourceElement, TTargetElement, TData> relation)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    {
        throw new NotImplementedException();
    }
}
