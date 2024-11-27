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
    private ElementId? _selectedMethodId;

    public DotNetMethodBrowser(DotNetSolutionContext solutionContext, DotNetTypes dotNetTypes)
    {
        _solutionContext = solutionContext;
        _dotNetTypes = dotNetTypes;

        _slice = solutionContext.LazySlice;
    }

    public async Task<IModel> InitAsync()
    {
        return await DisplayMethodsOrFlowGraphAsync();
    }

    public async Task<IModel?> ProcessCommandAsync(Command command)
    {
        if (command.Name == _openActionName && 
            command.Parameters.TryGetValue(_idActionParameterName, out var id))
        {
            _selectedMethodId = new ElementId(id);
            return await DisplayMethodsOrFlowGraphAsync();
        }

        return null;
    }

    private async Task<IModel> DisplayMethodsOrFlowGraphAsync()
    {
        // If a method is selected, try to display its flow graph
        if (_selectedMethodId is not null)
        {
            var flowGraph = _solutionContext.TryGetFlowGraph(_selectedMethodId.Value);
            if (flowGraph is not null)
            {
                var flowGraphBrowser = new SampleFlowGraphBrowser(flowGraph);
                return await flowGraphBrowser.InitAsync();
            }
        }

        // Otherwise, display the list of all methods
        return await DisplayMethodListAsync();
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
