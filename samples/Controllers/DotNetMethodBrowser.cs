using Microsoft.CodeAnalysis;
using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.DotNet;

using System.Collections.Immutable;

namespace Controllers;

public class DotNetMethodBrowser : IController
{
    private const string _openActionName = "Open";
    private const string _idActionParameterName = "Id";

    private readonly DotNetSolutionContext _solutionContext;
    private readonly DotNetTypes _dotNetTypes;

    private readonly ILazySlice _slice;

    public DotNetMethodBrowser(DotNetSolutionContext solutionContext, DotNetTypes dotNetTypes)
    {
        _solutionContext = solutionContext;
        _dotNetTypes = dotNetTypes;

        _slice = solutionContext.LazySlice;
    }

    public async Task<IModel> InitAsync()
    {
        return await DisplayMethodListAsync();
    }

    public async Task<IModel?> ProcessCommandAsync(Command command)
    {
        if (command.Name == _openActionName && 
            command.Parameters.TryGetValue(_idActionParameterName, out var id))
        {
            return await DisplayFlowGraphAsync(new ElementId(id));
        }

        return null;
    }

    private Task<IModel> DisplayFlowGraphAsync(ElementId id)
    {
        var flowGraph = _solutionContext.TryGetFlowGraph(id)
            ?? throw new Exception($"Flow graph for method {id} not found");

        var result = FlowGraphHelper.CreateGraphModel(flowGraph);

        return Task.FromResult<IModel>(result);
        }

    private async Task<IModel> DisplayMethodListAsync()
    {
        var nodes = new List<Node>();
        var edges = new List<Edge>();

        var methods = await DotNetMethodHelper.GetAllMethodsWithDisplayNamesAsync(_slice, _dotNetTypes);
        
        foreach ((var method, var displayName) in methods)
        {
            nodes.Add(new Node(
                method.Id.Value,
                displayName,
                CreateOpenCommand(method)));
        }

        return new Graph([.. nodes], [.. edges]);
    }

    private static Command CreateOpenCommand(ElementInfo element)
    {
        return new Command(
            _openActionName, 
            ImmutableDictionary<string, string>.Empty.Add(_idActionParameterName, element.Id.Value));
    }
}
