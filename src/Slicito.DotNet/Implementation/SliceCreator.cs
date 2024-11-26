using System.Collections.Concurrent;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.DotNet.Implementation;
internal class SliceCreator
{
    private readonly Solution _solution;
    private readonly DotNetTypes _types;
    private readonly ISliceManager _sliceManager;

    private readonly ElementCache _elementCache;
    private readonly ConcurrentDictionary<IMethodSymbol, IFlowGraph?> _flowGraphCache = [];

    public ILazySlice LazySlice { get; }

    public SliceCreator(Solution solution, DotNetTypes types, ISliceManager sliceManager)
    {
        _solution = solution;
        _types = types;
        _sliceManager = sliceManager;

        _elementCache = new(types);

        LazySlice = CreateSlice();
    }

    public IFlowGraph? TryCreateFlowGraph(ElementId elementId)
    {
        var element = _elementCache.TryGetSymbol(elementId);

        if (element is not IMethodSymbol method)
        {
            return null;
        }

        return _flowGraphCache.GetOrAdd(method, _ => FlowGraphCreator.TryCreateFlowGraph(method, _solution));
    }

    private ILazySlice CreateSlice()
    {
        var namespaceMemberTypes = _types.Namespace | _types.Type;
        var typeMemberTypes = _types.Type | _types.Property | _types.Field | _types.Method;

        var symbolTypes = _types.Namespace | _types.Type | _types.Property | _types.Field | _types.Method;

        return _sliceManager.CreateBuilder()
            .AddRootElements(_types.Project, LoadProjects)
            .AddHierarchyLinks(_types.Contains, _types.Project, _types.Namespace, LoadProjectNamespacesAsync)
            .AddHierarchyLinks(_types.Contains, _types.Namespace, namespaceMemberTypes, LoadNamespaceMembers)
            .AddHierarchyLinks(_types.Contains, _types.Type, typeMemberTypes, LoadTypeMembers)
            .AddElementAttribute(_types.Project, DotNetAttributeNames.Name, LoadProjectName)
            .AddElementAttribute(symbolTypes, DotNetAttributeNames.Name, LoadSymbolName)
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

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadNamespaceMembers(ElementId sourceId)
    {
        var @namespace = _elementCache.GetNamespace(sourceId);

        return @namespace.GetMembers()
            .Select(member => ToPartialLinkInfo(_elementCache.GetElement(member)));
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadTypeMembers(ElementId sourceId)
    {
        var type = _elementCache.GetType(sourceId);

        return type.GetMembers()
            .Select(member => ToPartialLinkInfo(_elementCache.GetElement(member)));
    }

    private string LoadProjectName(ElementId elementId) => Path.GetFileName(elementId.Value);

    private string LoadSymbolName(ElementId elementId) =>
        _elementCache.GetSymbol(elementId).Name;

    private static ISliceBuilder.PartialElementInfo ToPartialElementInfo(ElementInfo element) =>
        new(element.Id, element.Type);

    private static ISliceBuilder.PartialLinkInfo ToPartialLinkInfo(ElementInfo target) =>
        new(new(target.Id, target.Type));
}
