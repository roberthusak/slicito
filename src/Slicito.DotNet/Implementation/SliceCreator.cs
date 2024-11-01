using System.Collections.Concurrent;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;
internal class SliceCreator
{
    private readonly Solution _solution;
    private readonly DotNetTypes _types;
    private readonly ISliceManager _sliceManager;

    private readonly ConcurrentDictionary<ElementId, object> _elementRoslynObjects = [];

    private SliceCreator(Solution solution, DotNetTypes types, ISliceManager sliceManager)
    {
        _solution = solution;
        _types = types;
        _sliceManager = sliceManager;
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
            .Select(project => SaveAndReturnElementInfo(ElementIdProvider.GetElementId(project), project));

    private async ValueTask<IEnumerable<ISliceBuilder.PartialLinkInfo>> LoadProjectNamespacesAsync(ElementId sourceId)
    {
        var project = (Project) _elementRoslynObjects[sourceId];

        var compilation = await project.GetCompilationAsync()
            ?? throw new InvalidOperationException(
                $"The project '{project.FilePath}' could not be loaded into a Roslyn Compilation.");

        return compilation.SourceModule.GlobalNamespace.GetMembers()
            .OfType<INamespaceSymbol>()
            .Select(namespaceSymbol => SaveAndReturnLinkInfo(ElementIdProvider.GetElementId(sourceId, namespaceSymbol), namespaceSymbol));
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadNamespaceNamespacesAndTypes(ElementId sourceId)
    {
        var @namespace = (INamespaceSymbol) _elementRoslynObjects[sourceId];

        return @namespace.GetMembers()
            .Select(member =>
            {
                switch (member)
                {
                    case INamespaceSymbol nestedNamespace:
                        var targetNamespaceInfo = new ElementInfo(
                            ElementIdProvider.GetElementId(sourceId, nestedNamespace),
                            _types.Namespace);
                        return SaveAndReturnLinkInfo(targetNamespaceInfo, nestedNamespace);

                    case ITypeSymbol type:
                        var targetTypeInfo = new ElementInfo(
                            ElementIdProvider.GetElementId(sourceId, type),
                            _types.Type);
                        return SaveAndReturnLinkInfo(targetTypeInfo, type);

                    default:
                        throw new InvalidOperationException(
                            $"Unexpected member symbol type '{member.GetType()}' in namespace '{@namespace.Name}'.");
                }
            });
    }

    private string LoadProjectName(ElementId elementId) => Path.GetFileName(elementId.Value);

    private string LoadNamespaceName(ElementId elementId)
    {
        var @namespace = (INamespaceSymbol) _elementRoslynObjects[elementId];

        return @namespace.Name;
    }

    private string LoadTypeName(ElementId elementId)
    {
        var type = (ITypeSymbol) _elementRoslynObjects[elementId];

        return type.Name;
    }

    private ISliceBuilder.PartialElementInfo SaveAndReturnElementInfo(ElementId id, object roslynObject)
    {
        SaveElementIdMapping(id, roslynObject);

        return new(id);
    }

    private ISliceBuilder.PartialLinkInfo SaveAndReturnLinkInfo(ElementId targetId, object roslynObject)
    {
        SaveElementIdMapping(targetId, roslynObject);

        return new(new(targetId));
    }

    private ISliceBuilder.PartialLinkInfo SaveAndReturnLinkInfo(ElementInfo target, object roslynObject)
    {
        SaveElementIdMapping(target.Id, roslynObject);

        return new(new(target.Id, target.Type));
    }

    private void SaveElementIdMapping(ElementId id, object roslynObject)
    {
        _elementRoslynObjects.AddOrUpdate(id, roslynObject, (_, existing) =>
        {
            if (!existing.Equals(roslynObject))
            {
                throw new InvalidOperationException(
                    $"Element ID '{id}' is already mapped to a different object ({existing}) than the one to store ({roslynObject}).");
            }

            return existing;
        });
    }
}
