using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.Abstractions.Queries;
using Slicito.DotNet;

namespace Controllers;

public static class DotNetMethodHelper
{
    public static async Task<IEnumerable<(ElementInfo Method, string DisplayName)>> GetAllMethodsWithDisplayNamesAsync(
        ILazySlice slice,
        DotNetTypes dotNetTypes)
    {
        var methods = new List<(ElementInfo Method, string DisplayName)>();
        var nameProvider = slice.GetElementAttributeProviderAsyncCallback(DotNetAttributeNames.Name);
        
        // Get all projects (root elements)
        var projects = await slice.GetRootElementsAsync();
        var hierarchyExplorer = slice.GetLinkExplorer(slice.Schema.HierarchyLinkType!);
        
        foreach (var project in projects)
        {
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
                    var methodElements = members.Where(m =>
                        m.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Method.Value));

                    foreach (var method in methodElements)
                    {
                        var methodName = await nameProvider(method.Id);
                        var typeName = await nameProvider(type.Id);
                        var namespaceName = await nameProvider(ns.Id);
                        
                        var displayName = $"{namespaceName}.{typeName}.{methodName}";
                        methods.Add((method, displayName));
                    }
                }
            }
        }

        return methods;
    }
} 
