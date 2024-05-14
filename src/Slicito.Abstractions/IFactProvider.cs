namespace Slicito.Abstractions;

public interface IFactProvider
{
    Task<IFactQueryResult> QueryAsync(IFactQuery query);
}
