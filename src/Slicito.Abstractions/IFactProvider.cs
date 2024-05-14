namespace Slicito.Abstractions;

public interface IFactProvider
{
    Task<FactQueryResult> QueryAsync(FactQuery query);
}
