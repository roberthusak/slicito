namespace Slicito.Abstractions.Relations;

public static class Relation
{
    public static IBinaryRelation<TSourceElement, TTargetElement, TDataTo>
        Merge<TSourceElement, TTargetElement, TDataFrom, TDataTo>(
            IEnumerable<IBinaryRelation<TSourceElement, TTargetElement, TDataFrom>> relations,
            Func<TDataFrom, TDataTo> detailTransformer)
        where TSourceElement : class, IElement
        where TTargetElement : class, IElement
    {
        throw new NotImplementedException();
    }

    public static IBinaryRelation<TSourceElement, TTargetElement, EmptyStruct>
        Merge<TSourceElement, TTargetElement, TData>(
            IEnumerable<IBinaryRelation<TSourceElement, TTargetElement, TData>> relations)
    where TSourceElement : class, IElement
    where TTargetElement : class, IElement
        => Merge(relations, _ => new EmptyStruct());
}
