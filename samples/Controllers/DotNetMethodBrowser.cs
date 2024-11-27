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
    private readonly ILazySlice _slice;
    private ElementId? _selectedMethodId;

    public DotNetMethodBrowser(DotNetSolutionContext solutionContext)
    {
        _solutionContext = solutionContext;
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

        // Get all projects (root elements)
        var projects = await _slice.GetRootElementsAsync();
        var nameProvider = _slice.GetElementAttributeProviderAsyncCallback(DotNetAttributeNames.Name);
        
        foreach (var project in projects)
        {
            // Traverse the hierarchy: Project -> Namespace -> Type -> Method
            var hierarchyExplorer = _slice.GetLinkExplorer(_slice.Schema.HierarchyLinkType!);
            
            // Get namespaces in the project
            var namespaces = await hierarchyExplorer.GetTargetElementsAsync(project.Id);
            foreach (var ns in namespaces)
            {
                // Get types in the namespace
                var types = await hierarchyExplorer.GetTargetElementsAsync(ns.Id);
                foreach (var type in types)
                {
                    // Get methods in the type
                    var members = await hierarchyExplorer.GetTargetElementsAsync(type.Id);
                    var methods = members.Where(m => m.Type.Value.AttributeValues
                        .TryGetValue(DotNetAttributeNames.Kind, out var kinds) && 
                        kinds.Contains("Method"));

                    foreach (var method in methods)
                    {
                        var methodName = await nameProvider(method.Id);
                        var typeName = await nameProvider(type.Id);
                        var namespaceName = await nameProvider(ns.Id);
                        
                        var displayName = $"{namespaceName}.{typeName}.{methodName}";
                        
                        nodes.Add(new Node(
                            method.Id.Value,
                            displayName,
                            CreateOpenCommand(method)));
                    }
                }
            }
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
