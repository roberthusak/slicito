using System.Collections.Concurrent;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;

using Slicito.Abstractions;

namespace Slicito.DotNet.Implementation;

internal class ElementCache
{
    /// <remarks>
    /// If <see cref="AssemblyReferencePath"/> is <see langword="null"/>, the symbol is defined in the <see cref="RelatedProject"/>.
    /// Otherwise, the symbol is defined in an assembly referenced by the <see cref="RelatedProject"/>, and the <see cref="AssemblyReferencePath"/>
    /// is the path to the assembly file.
    /// </remarks>
    public record struct SymbolInfo(Project RelatedProject, string? AssemblyReferencePath, ISymbol Symbol);

    private readonly DotNetTypes _types;

    private readonly ConcurrentDictionary<ElementId, Solution> _solutionById = [];
    private readonly ConcurrentDictionary<ElementId, Project> _projectById = [];
    private readonly ConcurrentDictionary<ElementId, SymbolInfo> _symbolInfoById = [];

    private readonly ConcurrentDictionary<ISymbol, SymbolInfo> _symbolInfoCache = [];

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

    public async ValueTask<ElementInfo> GetElementAsync(Project relatedProject, ISymbol symbol)
    {
        var info = await GenerateSymbolInfoAsync(relatedProject, symbol);

        var id = ElementIdProvider.GetId(info.RelatedProject, info.AssemblyReferencePath, info.Symbol);

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

    private async ValueTask<SymbolInfo> GenerateSymbolInfoAsync(Project relatedProject, ISymbol symbol)
    {
        if (_symbolInfoCache.TryGetValue(symbol, out var info))
        {
            return info;
        }

        var compilation = await relatedProject.GetCompilationAsync()
            ?? throw new InvalidOperationException($"Project '{relatedProject}' has no compilation.");

        string? assemblyReferencePath = null;
        ISymbol resultSymbol;

        var sourceSymbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, relatedProject.Solution);
        if (sourceSymbol is not null)
        {
            if (!compilation.Assembly.Equals(sourceSymbol.ContainingAssembly, SymbolEqualityComparer.Default))
            {
                relatedProject = relatedProject.Solution.GetProject(sourceSymbol.ContainingAssembly)
                    ?? throw new InvalidOperationException($"Project for symbol '{symbol}' not found.");
            }

            resultSymbol = sourceSymbol;
        }
        else
        {
            var metadataReference = compilation.GetMetadataReference(symbol.ContainingAssembly)
                ?? throw new InvalidOperationException($"Metadata reference for symbol '{symbol}' not found.");

            var peReference = metadataReference as PortableExecutableReference
                ?? throw new InvalidOperationException($"Metadata reference for symbol '{symbol}' is not a portable executable reference.");

            assemblyReferencePath = peReference.FilePath;
            resultSymbol = symbol;
        }

        var result = new SymbolInfo(relatedProject, assemblyReferencePath, resultSymbol);

        _symbolInfoCache.AddOrUpdate(symbol, result, (_, existing) =>
        {
            if (existing != result)
            {
                throw new InvalidOperationException($"Symbol ID '{symbol}' is already mapped to a different symbol information ({existing}).");
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

    public ISymbol GetSymbolAndRelatedProject(ElementId id, out Project relatedProject) => GetSymbolAndRelatedProject<ISymbol>(id, out relatedProject);

    public ISymbol? TryGetSymbol(ElementId id) =>
        _symbolInfoById.TryGetValue(id, out var value) ? value.Symbol : null;

    public Project? TryGetProject(ElementId id) =>
        _symbolInfoById.TryGetValue(id, out var value) ? value.RelatedProject : null;

    public INamespaceSymbol GetNamespace(ElementId id) => (INamespaceSymbol) _symbolInfoById[id].Symbol;

    public INamespaceSymbol GetNamespaceAndRelatedProject(ElementId id, out Project relatedProject) => GetSymbolAndRelatedProject<INamespaceSymbol>(id, out relatedProject);

    public ITypeSymbol GetType(ElementId id) => (ITypeSymbol) _symbolInfoById[id].Symbol;

    public ITypeSymbol GetTypeAndRelatedProject(ElementId id, out Project relatedProject) => GetSymbolAndRelatedProject<ITypeSymbol>(id, out relatedProject);

    public IPropertySymbol GetProperty(ElementId id) => (IPropertySymbol) _symbolInfoById[id].Symbol;

    public IPropertySymbol GetPropertyAndRelatedProject(ElementId id, out Project relatedProject) => GetSymbolAndRelatedProject<IPropertySymbol>(id, out relatedProject);

    public IMethodSymbol GetMethod(ElementId id) => (IMethodSymbol) _symbolInfoById[id].Symbol;

    public IMethodSymbol GetMethodAndRelatedProject(ElementId id, out Project relatedProject) => GetSymbolAndRelatedProject<IMethodSymbol>(id, out relatedProject);

    private TSymbol GetSymbolAndRelatedProject<TSymbol>(ElementId id, out Project project)
        where TSymbol : ISymbol
    {
        var info = _symbolInfoById[id];
        project = info.RelatedProject;
        return (TSymbol) info.Symbol;
    }
}
