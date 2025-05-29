using System.Collections.Concurrent;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;

internal class ElementCache
{
    public record struct SymbolInfo(Project Project, ISymbol Symbol);

    private readonly DotNetTypes _types;

    private readonly ConcurrentDictionary<ElementId, Solution> _solutionById = [];
    private readonly ConcurrentDictionary<ElementId, Project> _projectById = [];
    private readonly ConcurrentDictionary<ElementId, SymbolInfo> _symbolInfoById = [];

    private readonly ConcurrentDictionary<ISymbol, SymbolInfo> _translatedSymbolsCache = [];

    public ElementCache(DotNetTypes types)
    {
        _types = types;
    }

    public ElementInfo GetElement(Solution solution)
    {
        var id = ElementIdProvider.GetId(solution);

        _solutionById.AddOrUpdate(id, solution, (_, existing) =>
        {
            if (existing != solution)
            {
                throw new InvalidOperationException($"Solution ID '{id}' is already mapped to a different solution ({existing}).");
            }

            return existing;
        });

        return new(id, _types.Solution);
    }

    public ElementInfo GetElement(Project project)
    {
        var id = ElementIdProvider.GetId(project);

        _projectById.AddOrUpdate(id, project, (_, existing) =>
        {
            if (existing != project)
            {
                throw new InvalidOperationException($"Project ID '{id}' is already mapped to a different project ({existing}).");
            }

            return existing;
        });

        return new(id, _types.Project);
    }

    public async ValueTask<ElementInfo> GetElementAsync(Project projectHint, ISymbol symbol)
    {
        var info = await TranslateToSourceSymbolAsync(projectHint, symbol);

        var id = ElementIdProvider.GetId(info.Project, info.Symbol);

        _symbolInfoById.AddOrUpdate(id, info, (_, existing) =>
        {
            if (existing != info)
            {
                throw new InvalidOperationException($"Symbol ID '{id}' is already mapped to a different symbol ({existing}).");
            }

            return existing;
        });

        return new(id, GetSymbolElementType(info.Symbol));
    }

    private async ValueTask<SymbolInfo> TranslateToSourceSymbolAsync(Project projectHint, ISymbol symbol)
    {
        if (_translatedSymbolsCache.TryGetValue(symbol, out var info))
        {
            return info;
        }

        var sourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, projectHint.Solution)
            ?? throw new InvalidOperationException($"Symbol '{symbol}' has no source definition.");

        Project project;
        if (projectHint.TryGetCompilation(out var compilation) &&
            compilation.Assembly.Equals(sourceSymbol.ContainingAssembly, SymbolEqualityComparer.Default))
        {
            project = projectHint;
        }
        else
        {
            project = projectHint.Solution.GetProject(sourceSymbol.ContainingAssembly)
                ?? throw new InvalidOperationException($"Project for symbol '{symbol}' not found.");
        }

        var result = new SymbolInfo(project, sourceSymbol);

        _translatedSymbolsCache.AddOrUpdate(symbol, result, (_, existing) =>
        {
            if (existing != result)
            {
                throw new InvalidOperationException($"Symbol ID '{symbol}' is already mapped to a different symbol ({existing}).");
            }

            return existing;
        });

        return result;
    }

    private ElementType GetSymbolElementType(ISymbol symbol)
    {
        return symbol switch
        {
            INamespaceSymbol @namespace => _types.Namespace,
            ITypeSymbol type => _types.Type,
            IPropertySymbol property => _types.Property,
            IFieldSymbol field => _types.Field,
            IMethodSymbol method => _types.Method,
            _ => throw new InvalidOperationException($"Unexpected symbol kind: {symbol.Kind}."),
        };
    }

    public Solution GetSolution(ElementId id) => _solutionById[id];

    public Project GetProject(ElementId id) => _projectById[id];

    public ISymbol GetSymbol(ElementId id) => _symbolInfoById[id].Symbol;

    public ISymbol GetSymbolAndProject(ElementId id, out Project project) => GetSymbolAndProject<ISymbol>(id, out project);

    public ISymbol? TryGetSymbol(ElementId id) =>
        _symbolInfoById.TryGetValue(id, out var value) ? value.Symbol : null;

    public Project? TryGetProject(ElementId id) =>
        _symbolInfoById.TryGetValue(id, out var value) ? value.Project : null;

    public INamespaceSymbol GetNamespace(ElementId id) => (INamespaceSymbol) _symbolInfoById[id].Symbol;

    public INamespaceSymbol GetNamespaceAndProject(ElementId id, out Project project) => GetSymbolAndProject<INamespaceSymbol>(id, out project);

    public ITypeSymbol GetType(ElementId id) => (ITypeSymbol) _symbolInfoById[id].Symbol;

    public ITypeSymbol GetTypeAndProject(ElementId id, out Project project) => GetSymbolAndProject<ITypeSymbol>(id, out project);

    public IPropertySymbol GetProperty(ElementId id) => (IPropertySymbol) _symbolInfoById[id].Symbol;

    public IPropertySymbol GetPropertyAndProject(ElementId id, out Project project) => GetSymbolAndProject<IPropertySymbol>(id, out project);

    public IMethodSymbol GetMethod(ElementId id) => (IMethodSymbol) _symbolInfoById[id].Symbol;

    public IMethodSymbol GetMethodAndProject(ElementId id, out Project project) => GetSymbolAndProject<IMethodSymbol>(id, out project);

    private TSymbol GetSymbolAndProject<TSymbol>(ElementId id, out Project project)
        where TSymbol : ISymbol
    {
        var info = _symbolInfoById[id];
        project = info.Project;
        return (TSymbol) info.Symbol;
    }
}
