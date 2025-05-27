using Slicito.DotNet.Facts;

namespace Slicito.DotNet;

public static class DotNetSliceFragmentExtensions
{
    public static async ValueTask<IEnumerable<ICSharpNamespaceElement>> GetAllContainedNamespacesAsync(
        this IDotNetSliceFragment fragment,
        ICSharpProjectElement project)
    {
        var topLevelNamespaces = await fragment.GetNamespacesAsync(project);
        var nestedNamespaces = await Task.WhenAll(
            topLevelNamespaces.Select(ns => GetAllContainedNamespacesRecursiveAsync(fragment, ns).AsTask()));
        
        return topLevelNamespaces.Concat(nestedNamespaces.SelectMany(ns => ns));
    }

    public static async ValueTask<IEnumerable<ICSharpNamespaceElement>> GetAllContainedNamespacesAsync(
        this IDotNetSliceFragment fragment,
        ICSharpNamespaceElement @namespace)
    {
        return await GetAllContainedNamespacesRecursiveAsync(fragment, @namespace);
    }

    public static async ValueTask<IEnumerable<ICSharpTypeElement>> GetAllContainedTypesAsync(
        this IDotNetSliceFragment fragment,
        ICSharpProjectElement project)
    {
        var topLevelNamespaces = await fragment.GetNamespacesAsync(project);
        var typesFromNamespaces = await Task.WhenAll(
            topLevelNamespaces.Select(ns => GetAllContainedTypesRecursiveAsync(fragment, ns).AsTask()));
        
        return typesFromNamespaces.SelectMany(types => types);
    }

    public static async ValueTask<IEnumerable<ICSharpTypeElement>> GetAllContainedTypesAsync(
        this IDotNetSliceFragment fragment,
        ICSharpNamespaceElement @namespace)
    {
        return await GetAllContainedTypesRecursiveAsync(fragment, @namespace);
    }

    public static async ValueTask<IEnumerable<ICSharpTypeElement>> GetAllContainedTypesAsync(
        this IDotNetSliceFragment fragment,
        ICSharpTypeElement type)
    {
        return await GetAllContainedTypesRecursiveAsync(fragment, type);
    }

    public static async ValueTask<IEnumerable<ICSharpMethodElement>> GetAllContainedMethodsAsync(
        this IDotNetSliceFragment fragment,
        ICSharpProjectElement project)
    {
        var topLevelNamespaces = await fragment.GetNamespacesAsync(project);
        var typesFromNamespaces = await Task.WhenAll(
            topLevelNamespaces.Select(ns => GetAllContainedTypesRecursiveAsync(fragment, ns).AsTask()));
        var methodsFromTypes = await Task.WhenAll(
            typesFromNamespaces.SelectMany(types => types)
                .Select(type => GetAllContainedMethodsRecursiveAsync(fragment, type).AsTask()));
        
        return methodsFromTypes.SelectMany(methods => methods);
    }

    public static async ValueTask<IEnumerable<ICSharpMethodElement>> GetAllContainedMethodsAsync(
        this IDotNetSliceFragment fragment,
        ICSharpNamespaceElement @namespace)
    {
        var types = await GetAllContainedTypesRecursiveAsync(fragment, @namespace);
        var methodsFromTypes = await Task.WhenAll(
            types.Select(type => GetAllContainedMethodsRecursiveAsync(fragment, type).AsTask()));
        
        return methodsFromTypes.SelectMany(methods => methods);
    }

    public static async ValueTask<IEnumerable<ICSharpMethodElement>> GetAllContainedMethodsAsync(
        this IDotNetSliceFragment fragment,
        ICSharpTypeElement type)
    {
        return await GetAllContainedMethodsRecursiveAsync(fragment, type);
    }
    
    private static async ValueTask<IEnumerable<ICSharpNamespaceElement>> GetAllContainedNamespacesRecursiveAsync(
        this IDotNetSliceFragment fragment,
        ICSharpNamespaceElement @namespace)
    {
        var directNamespaces = await fragment.GetNamespacesAsync(@namespace);
        var nestedNamespaces = await Task.WhenAll(
            directNamespaces.Select(ns => GetAllContainedNamespacesRecursiveAsync(fragment, ns).AsTask()));
        
        return directNamespaces.Concat(nestedNamespaces.SelectMany(ns => ns));
    }

    private static async ValueTask<IEnumerable<ICSharpTypeElement>> GetAllContainedTypesRecursiveAsync(
        this IDotNetSliceFragment fragment,
        ICSharpNamespaceElement @namespace)
    {
        var directTypes = await fragment.GetTypesAsync(@namespace);
        var nestedNamespaces = await fragment.GetNamespacesAsync(@namespace);
        
        // Get types from nested namespaces
        var typesFromNestedNamespaces = await Task.WhenAll(
            nestedNamespaces.Select(ns => GetAllContainedTypesRecursiveAsync(fragment, ns).AsTask()));
        
        // Get types nested within direct types
        var typesFromDirectTypes = await Task.WhenAll(
            directTypes.Select(t => GetAllContainedTypesRecursiveAsync(fragment, t).AsTask()));
        
        return directTypes
            .Concat(typesFromNestedNamespaces.SelectMany(types => types))
            .Concat(typesFromDirectTypes.SelectMany(types => types));
    }

    private static async ValueTask<IEnumerable<ICSharpTypeElement>> GetAllContainedTypesRecursiveAsync(
        this IDotNetSliceFragment fragment,
        ICSharpTypeElement type)
    {
        var directTypes = await fragment.GetTypesAsync(type);
        var nestedTypes = await Task.WhenAll(
            directTypes.Select(t => GetAllContainedTypesRecursiveAsync(fragment, t).AsTask()));
        
        return directTypes.Concat(nestedTypes.SelectMany(types => types));
    }

    private static async ValueTask<IEnumerable<ICSharpMethodElement>> GetAllContainedMethodsRecursiveAsync(
        this IDotNetSliceFragment fragment,
        ICSharpTypeElement type)
    {
        var directMethods = await fragment.GetMethodsAsync(type);
        var nestedTypes = await fragment.GetTypesAsync(type);
        var methodsFromNestedTypes = await Task.WhenAll(
            nestedTypes.Select(t => GetAllContainedMethodsRecursiveAsync(fragment, t).AsTask()));
        
        return directMethods.Concat(methodsFromNestedTypes.SelectMany(methods => methods));
    }
}
