using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.DotNet.Facts;
using Slicito.DotNet.Implementation.Facts;

namespace Slicito.DotNet.Implementation;

internal class DotNetSliceFragment(ISlice slice, DotNetTypes dotNetTypes) : IDotNetSliceFragment
{
    private readonly ILazyLinkExplorer _hierarchyExplorer = slice.GetLinkExplorer(dotNetTypes.Contains);

    private readonly Func<ElementId, ValueTask<string>> _nameProvider = slice.GetElementAttributeProviderAsyncCallback(DotNetAttributeNames.Name);

    public ISlice Slice { get; } = slice;

    public async ValueTask<IEnumerable<ISolutionElement>> GetSolutionsAsync()
    {
        var solutions = await Slice.GetRootElementsAsync(dotNetTypes.Solution);

        return solutions.Select(solution => new SolutionElement(solution.Id, GetName(solution.Id)));
    }

    public async ValueTask<IEnumerable<ICSharpProjectElement>> GetProjectsAsync(ISolutionElement solution)
    {
        var projects = await _hierarchyExplorer.GetTargetElementsAsync(solution.Id);

        return projects.Select(project => new CSharpProjectElement(project.Id, GetName(project.Id)));
    }

    public async ValueTask<IEnumerable<ICSharpNamespaceElement>> GetNamespacesAsync(ICSharpProjectElement project)
    {
        var namespaces = await _hierarchyExplorer.GetTargetElementsAsync(project.Id);

        return namespaces.Select(ns => new CSharpNamespaceElement(ns.Id, GetName(ns.Id)));
    }

    public async ValueTask<IEnumerable<ICSharpNamespaceElement>> GetNamespacesAsync(ICSharpNamespaceElement @namespace)
    {
        var namespaces = await _hierarchyExplorer.GetTargetElementsAsync(@namespace.Id);

        return namespaces
            .Where(ns => ns.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Namespace.Value))
            .Select(ns => new CSharpNamespaceElement(ns.Id, GetName(ns.Id)));
    }

    public async ValueTask<IEnumerable<ICSharpTypeElement>> GetTypesAsync(ICSharpNamespaceElement @namespace)
    {
        var types = await _hierarchyExplorer.GetTargetElementsAsync(@namespace.Id);

        return types
            .Where(type => type.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Type.Value))
            .Select(type => new CSharpTypeElement(type.Id, GetName(type.Id)));
    }

    public async ValueTask<IEnumerable<ICSharpTypeElement>> GetTypesAsync(ICSharpTypeElement type)
    {
        var types = await _hierarchyExplorer.GetTargetElementsAsync(type.Id);

        return types
            .Where(type => type.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Type.Value))
            .Select(type => new CSharpTypeElement(type.Id, GetName(type.Id)));
    }

    public async ValueTask<IEnumerable<ICSharpPropertyElement>> GetPropertiesAsync(ICSharpTypeElement type)
    {
        var properties = await _hierarchyExplorer.GetTargetElementsAsync(type.Id);

        return properties
            .Where(property => property.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Property.Value))
            .Select(property => new CSharpPropertyElement(property.Id, GetName(property.Id)));
    }

    public async ValueTask<IEnumerable<ICSharpPropertyElement>> GetPropertiesAsync(ICSharpPropertyElement property)
    {
        var properties = await _hierarchyExplorer.GetTargetElementsAsync(property.Id);

        return properties
            .Where(property => property.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Property.Value))
            .Select(property => new CSharpPropertyElement(property.Id, GetName(property.Id)));
    }

    public async ValueTask<IEnumerable<ICSharpMethodElement>> GetMethodsAsync(ICSharpTypeElement type)
    {
        var methods = await _hierarchyExplorer.GetTargetElementsAsync(type.Id);

        return methods
            .Where(method => method.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Method.Value))
            .Select(method => new CSharpMethodElement(method.Id, GetName(method.Id)));
    }

    public async ValueTask<IEnumerable<ICSharpOperationElement>> GetOperationsAsync(ICSharpMethodElement method)
    {
        var operations = await _hierarchyExplorer.GetTargetElementsAsync(method.Id);

        return operations
            .Where(operation => operation.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Operation.Value))
            .Select(operation => CreateOperationElement(operation));
    }

    private string GetName(ElementId elementId)
    {
        var nameTask = _nameProvider(elementId);

        return nameTask.IsCompletedSuccessfully
            ? nameTask.Result
            : throw new InvalidOperationException("Unexpected asynchronous or failed name provider.", nameTask.AsTask().Exception);
    }

    private ICSharpOperationElement CreateOperationElement(ElementInfo element)
    {
        if (element.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Assignment.Value))
        {
            return new CSharpAssignmentElement(element.Id);
        }

        if (element.Type.Value.IsSubsetOfOrEquals(dotNetTypes.ConditionalJump.Value))
        {
            return new CSharpConditionalJumpElement(element.Id);
        }

        if (element.Type.Value.IsSubsetOfOrEquals(dotNetTypes.Call.Value))
        {
            return new CSharpCallElement(element.Id);
        }

        throw new InvalidOperationException($"Unsupported operation type: {element.Type.Value}");
    }
}
