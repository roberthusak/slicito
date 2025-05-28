using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Abstractions.Interaction;
using Slicito.DotNet.Facts;
using Slicito.DotNet.Implementation.Facts;

namespace Slicito.DotNet.Implementation;

internal class DotNetSliceFragment(ISlice slice, DotNetTypes dotNetTypes) : IDotNetSliceFragment
{
    private readonly ILazyLinkExplorer _hierarchyExplorer = slice.GetLinkExplorer(dotNetTypes.Contains);
    private readonly ILazyLinkExplorer _referenceExplorer = slice.GetLinkExplorer(dotNetTypes.References);

    private readonly Func<ElementId, ValueTask<string>> _nameProvider = slice.GetElementAttributeProviderAsyncCallback(DotNetAttributeNames.Name);
    private readonly Func<ElementId, ValueTask<string>> _outputKindProvider = slice.GetElementAttributeProviderAsyncCallback(DotNetAttributeNames.OutputKind);
    private readonly Func<ElementId, ValueTask<string>> _codeLocationProvider = slice.GetElementAttributeProviderAsyncCallback(CommonAttributeNames.CodeLocation);

    public ISlice Slice { get; } = slice;

    public async ValueTask<IEnumerable<ISolutionElement>> GetSolutionsAsync()
    {
        var solutions = await Slice.GetRootElementsAsync(dotNetTypes.Solution);

        return solutions.Select(solution => new SolutionElement(solution.Id, GetName(solution.Id)));
    }

    public async ValueTask<IEnumerable<ICSharpProjectElement>> GetProjectsAsync(ISolutionElement solution)
    {
        var projects = await _hierarchyExplorer.GetTargetElementsAsync(solution.Id);

        return projects.Select(project => new CSharpProjectElement(project.Id, GetName(project.Id), GetOutputKind(project.Id)));
    }

    public async ValueTask<IEnumerable<ICSharpProjectElement>> GetReferencedProjectsAsync(ICSharpProjectElement project)
    {
        var referencedProjects = await _referenceExplorer.GetTargetElementsAsync(project.Id);

        return referencedProjects.Select(referencedProject => new CSharpProjectElement(referencedProject.Id, GetName(referencedProject.Id), GetOutputKind(referencedProject.Id)));
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

        if (!nameTask.IsCompletedSuccessfully)
        {
            throw new InvalidOperationException("Unexpected asynchronous or failed name provider.", nameTask.AsTask().Exception);
        }

        return nameTask.Result;
    }

    private ProjectOutputKind GetOutputKind(ElementId id)
    {
        var outputKindTask = _outputKindProvider(id);

        if (!outputKindTask.IsCompletedSuccessfully)
        {
            throw new InvalidOperationException("Unexpected asynchronous or failed output kind provider.", outputKindTask.AsTask().Exception);
        }

        if (!Enum.TryParse(outputKindTask.Result, out ProjectOutputKind outputKind))
        {
            throw new InvalidOperationException($"Invalid project output kind: {outputKindTask.Result}");
        }

        return outputKind;
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

    public async ValueTask<CodeLocation> GetCodeLocationAsync(ICSharpTypeElement type) => await GetCodeLocationCommonAsync(type.Id);

    public async ValueTask<CodeLocation> GetCodeLocationAsync(ICSharpPropertyElement property) => await GetCodeLocationCommonAsync(property.Id);

    public async ValueTask<CodeLocation> GetCodeLocationAsync(ICSharpFieldElement field) => await GetCodeLocationCommonAsync(field.Id);

    public async ValueTask<CodeLocation> GetCodeLocationAsync(ICSharpMethodElement method) => await GetCodeLocationCommonAsync(method.Id);

    private async ValueTask<CodeLocation> GetCodeLocationCommonAsync(ElementId elementId)
    {
        var codeLocationTask = await _codeLocationProvider(elementId);

        return CodeLocation.Parse(codeLocationTask)
            ?? throw new InvalidOperationException($"Invalid code location format: '{codeLocationTask}'");
    }
}
