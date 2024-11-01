using System.Collections.Concurrent;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;
internal class SliceCreator
{
    private readonly Solution _solution;
    private readonly DotNetTypes _types;
    private readonly ISliceManager _sliceManager;

    private readonly ElementCache _elementCache;

    private SliceCreator(Solution solution, DotNetTypes types, ISliceManager sliceManager)
    {
        _solution = solution;
        _types = types;
        _sliceManager = sliceManager;

        _elementCache = new(types);
    }

    public static ILazySlice CreateSlice(Solution solution, DotNetTypes types, ISliceManager sliceManager)
    {
        var creator = new SliceCreator(solution, types, sliceManager);

        return creator.CreateSlice();
    }

    private ILazySlice CreateSlice()
    {
        return _sliceManager.CreateBuilder()
            .AddRootElements(_types.Project, LoadProjects)
            .AddHierarchyLinks(_types.Contains, _types.Project, _types.Namespace, LoadProjectNamespacesAsync)
            .AddHierarchyLinks(_types.Contains, _types.Namespace, _types.Namespace | _types.Type, LoadNamespaceNamespacesAndTypes)
            .AddElementAttribute(_types.Project, DotNetAttributeNames.Name, LoadProjectName)
            .AddElementAttribute(_types.Namespace, DotNetAttributeNames.Name, LoadNamespaceName)
            .AddElementAttribute(_types.Type, DotNetAttributeNames.Name, LoadTypeName)
            .BuildLazy();
    }

    private IEnumerable<ISliceBuilder.PartialElementInfo> LoadProjects() =>
        _solution.Projects
            .Select(project => ToPartialElementInfo(_elementCache.GetElement(project)));

    private async ValueTask<IEnumerable<ISliceBuilder.PartialLinkInfo>> LoadProjectNamespacesAsync(ElementId sourceId)
    {
        var project = _elementCache.GetProject(sourceId);

        var compilation = await project.GetCompilationAsync()
            ?? throw new InvalidOperationException(
                $"The project '{project.FilePath}' could not be loaded into a Roslyn Compilation.");

        return compilation.SourceModule.GlobalNamespace.GetMembers()
            .OfType<INamespaceSymbol>()
            .Select(namespaceSymbol => ToPartialLinkInfo(_elementCache.GetElement(namespaceSymbol)));
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadNamespaceNamespacesAndTypes(ElementId sourceId)
    {
        var @namespace = _elementCache.GetNamespace(sourceId);

        return @namespace.GetMembers()
            .Select(member => ToPartialLinkInfo(_elementCache.GetElement(member)));
    }

    private string LoadProjectName(ElementId elementId) => Path.GetFileName(elementId.Value);

    private string LoadNamespaceName(ElementId elementId) =>
        _elementCache.GetNamespace(elementId).Name;

    private string LoadTypeName(ElementId elementId) =>
        _elementCache.GetType(elementId).Name;

    private static ISliceBuilder.PartialElementInfo ToPartialElementInfo(ElementInfo element) =>
        new(element.Id, element.Type);

    private static ISliceBuilder.PartialLinkInfo ToPartialLinkInfo(ElementInfo target) =>
        new(new(target.Id, target.Type));
}
