using System.Collections.Concurrent;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;

internal class ElementCache
{
    private readonly DotNetTypes _types;

    private readonly ConcurrentDictionary<ElementId, object> _elementRoslynObjects = [];

    public ElementCache(DotNetTypes types)
    {
        _types = types;
    }

    public ElementInfo GetElement(object roslynObject)
    {
        return roslynObject switch
        {
            Project project => GetElement(project),
            INamespaceSymbol @namespace => GetElement(@namespace),
            ITypeSymbol type => GetElement(type),
            IPropertySymbol property => GetElement(property),
            IFieldSymbol field => GetElement(field),
            IMethodSymbol method => GetElement(method),
            _ => throw new InvalidOperationException(
                $"Unexpected type {roslynObject.GetType()} of the Roslyn object {roslynObject}."),
        };
    }

    public ElementInfo GetElement(Project project)
    {
        var id = ElementIdProvider.GetId(project);

        SaveElementIdMapping(id, project);

        return new(id, _types.Project);
    }

    public ElementInfo GetElement(INamespaceSymbol @namespace)
    {
        var id = ElementIdProvider.GetId(@namespace);

        SaveElementIdMapping(id, @namespace);

        return new(id, _types.Namespace);
    }

    public ElementInfo GetElement(ITypeSymbol type)
    {
        var id = ElementIdProvider.GetId(type);

        SaveElementIdMapping(id, type);

        return new(id, _types.Type);
    }

    public ElementInfo GetElement(IPropertySymbol property)
    {
        var id = ElementIdProvider.GetId(property);

        SaveElementIdMapping(id, property);

        return new(id, _types.Property);
    }

    public ElementInfo GetElement(IFieldSymbol field)
    {
        var id = ElementIdProvider.GetId(field);

        SaveElementIdMapping(id, field);

        return new(id, _types.Field);
    }

    public ElementInfo GetElement(IMethodSymbol method)
    {
        var id = ElementIdProvider.GetId(method);

        SaveElementIdMapping(id, method);

        return new(id, _types.Method);
    }

    public Project GetProject(ElementId id) => (Project) _elementRoslynObjects[id];

    public ISymbol GetSymbol(ElementId id) => (ISymbol) _elementRoslynObjects[id];

    public INamespaceSymbol GetNamespace(ElementId id) => (INamespaceSymbol) _elementRoslynObjects[id];

    public ITypeSymbol GetType(ElementId id) => (ITypeSymbol) _elementRoslynObjects[id];

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
