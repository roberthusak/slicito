using Slicito.Abstractions;
using Slicito.Abstractions.Queries;

namespace Slicito.DotNet;

public static class DotNetMethodHelper
{
    public static async Task<ElementInfo> FindSingleMethodAsync(
        ISlice slice,
        DotNetTypes dotNetTypes,
        string nameSuffix)
    {
        var methods = await GetAllMethodsWithDisplayNamesAsync(slice, dotNetTypes);

        return methods.Single(m => m.DisplayName.EndsWith(nameSuffix)).Method;
    }

    public static async Task<IEnumerable<(ElementInfo Method, string DisplayName)>> GetAllMethodsWithDisplayNamesAsync(
        ISlice slice,
        DotNetTypes dotNetTypes)
    {
        var methods = new List<(ElementInfo Method, string DisplayName)>();
        var nameProvider = slice.GetElementAttributeProviderAsyncCallback(DotNetAttributeNames.Name);
        var hierarchyExplorer = slice.GetLinkExplorer(slice.Schema.HierarchyLinkType!);

        // Get all projects (root elements)
        var projects = await slice.GetRootElementsAsync();

        foreach (var project in projects)
        {
            // Start processing from top-level namespaces
            var topLevelNamespaces = await hierarchyExplorer.GetTargetElementsAsync(project.Id);
            foreach (var ns in topLevelNamespaces)
            {
                await ProcessNamespaceAsync(ns, "", methods, hierarchyExplorer, nameProvider, dotNetTypes);
            }
        }

        return methods;
    }

    private static async Task ProcessNamespaceAsync(
        ElementInfo namespaceElement,
        string parentNamespace,
        List<(ElementInfo Method, string DisplayName)> methods,
        ILazyLinkExplorer hierarchyExplorer,
        Func<ElementId, ValueTask<string>> nameProvider,
        DotNetTypes dotNetTypes)
    {
        var namespaceName = await nameProvider(namespaceElement.Id);
        var fullNamespace = string.IsNullOrEmpty(parentNamespace)
            ? namespaceName
            : $"{parentNamespace}.{namespaceName}";

        var children = await hierarchyExplorer.GetTargetElementsAsync(namespaceElement.Id);

        foreach (var child in children)
        {
            // Check if child is a namespace or a type
            if (child.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Namespace.Value))
            {
                // Recursively process nested namespace
                await ProcessNamespaceAsync(child, fullNamespace, methods, hierarchyExplorer, nameProvider, dotNetTypes);
            }
            else if (child.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Type.Value))
            {
                // Process type and its nested types
                await ProcessTypeAsync(child, fullNamespace, "", methods, hierarchyExplorer, nameProvider, dotNetTypes);
            }
        }
    }

    private static async Task ProcessTypeAsync(
        ElementInfo typeElement,
        string namespacePrefix,
        string parentType,
        List<(ElementInfo Method, string DisplayName)> methods,
        ILazyLinkExplorer hierarchyExplorer,
        Func<ElementId, ValueTask<string>> nameProvider,
        DotNetTypes dotNetTypes)
    {
        var typeName = await nameProvider(typeElement.Id);
        var fullTypeName = string.IsNullOrEmpty(parentType)
            ? typeName
            : $"{parentType}+{typeName}";

        var members = await hierarchyExplorer.GetTargetElementsAsync(typeElement.Id);

        foreach (var member in members)
        {
            if (member.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Method.Value))
            {
                var methodName = await nameProvider(member.Id);
                var displayName = $"{namespacePrefix}.{fullTypeName}.{methodName}";
                methods.Add((Method: member, DisplayName: displayName));
            }
            else if (member.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Type.Value))
            {
                // Recursively process nested type
                await ProcessTypeAsync(member, namespacePrefix, fullTypeName, methods, hierarchyExplorer, nameProvider, dotNetTypes);
            }
        }
    }
}
