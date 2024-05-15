using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Implementation;

namespace Slicito.DotNet;

public class DotNetFactProvider : IFactProvider
{
    private readonly Solution _solution;

    public DotNetFactProvider(Solution solution)
    {
        _solution = solution;
    }

    public Task<FactQueryResult> QueryAsync(FactQuery query)
    {
        return FactQueryExecutor.ExecuteQuery(_solution, query);
    }
}
