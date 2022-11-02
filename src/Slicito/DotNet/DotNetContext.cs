using System.Collections.Immutable;
using System.Diagnostics;
using System.Web;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Slicito.Abstractions;
using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;
using Slicito.DotNet.Relations;

namespace Slicito.DotNet;

public partial class DotNetContext : IContext<DotNetElement, EmptyStruct>
{
    private readonly Dictionary<ISymbol, DotNetElement> _symbolsToElements;

    public IEnumerable<DotNetElement> Elements { get; }

    public IBinaryRelation<DotNetElement, DotNetElement, EmptyStruct> Hierarchy { get; }

    private DotNetContext(
        ImmutableArray<DotNetElement> elements,
        BinaryRelation<DotNetElement, DotNetElement, EmptyStruct> hierarchy,
        Dictionary<ISymbol, DotNetElement> symbolsToElements)
    {
        Elements = elements;
        Hierarchy = hierarchy;
        _symbolsToElements = symbolsToElements;
    }

    public DotNetElement? TryGetElementFromSymbol(ISymbol symbol) => _symbolsToElements.GetValueOrDefault(symbol);

    public DependencyRelations ExtractDependencyRelations(Predicate<DotNetElement>? filter = null)
    {
        var builder = new DependencyRelations.Builder();

        foreach (var element in Elements)
        {
            if (filter is not null && !filter(element))
            {
                continue;
            }

            switch (element)
            {
                case DotNetType typeElement:
                    ExtractTypeDependencies(typeElement, builder);
                    break;

                case DotNetMethod methodElement:
                    ExtractMethodDependencies(methodElement, builder);
                    break;

                case DotNetStorageTypeMember storageElement:
                    ExtractStorageMemberDependencies(storageElement, builder);
                    break;

                default:
                    break;
            }
        }

        return builder.Build();
    }

    private void ExtractTypeDependencies(DotNetType typeElement, DependencyRelations.Builder builder)
    {
        var baseSymbol = typeElement.Symbol.BaseType;
        if (baseSymbol is not null && TryGetElementFromSymbol(baseSymbol) is DotNetType baseElement)
        {
            builder.InheritsFrom.Add(typeElement, baseElement, default);
        }

        foreach (var @interface in typeElement.Symbol.AllInterfaces)
        {
            if (TryGetElementFromSymbol(@interface) is DotNetType interfaceElement)
            {
                builder.InheritsFrom.Add(typeElement, interfaceElement, default);
            }
        }
    }

    private void ExtractMethodDependencies(DotNetMethod methodElement, DependencyRelations.Builder builder)
    {
        var methodSymbol = methodElement.Symbol;

        var syntaxReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
        if (syntaxReference?.GetSyntax() is not BaseMethodDeclarationSyntax declaration)
        {
            return;
        }

        var overridenMethodSymbol = methodSymbol.OverriddenMethod;
        if (overridenMethodSymbol is not null
            && TryGetElementFromSymbol(overridenMethodSymbol) is DotNetMethod overridenMethodElement)
        {
            builder.Overrides.Add(methodElement, overridenMethodElement, default);
        }

        var projectElement = Hierarchy.GetAncestors(methodElement).OfType<DotNetProject>().First();

        var semanticModel = projectElement.Compilation.GetSemanticModel(syntaxReference.SyntaxTree);

        if (declaration is MethodDeclarationSyntax declarationWithReturnType
            && semanticModel.GetSymbolInfo(declarationWithReturnType.ReturnType).Symbol is ITypeSymbol returnTypeSymbol
            && TryGetElementFromSymbol(returnTypeSymbol) is DotNetType returnTypeElement)
        {
            builder.ReferencesType.Add(methodElement, returnTypeElement, declarationWithReturnType.ReturnType);
        }

        foreach (var parameter in declaration.ParameterList.Parameters)
        {
            if (semanticModel.GetDeclaredSymbol(parameter) is IParameterSymbol parameterSymbol
                && TryGetElementFromSymbol(parameterSymbol.Type) is DotNetType parameterTypeElement)
            {
                builder.ReferencesType.Add(methodElement, parameterTypeElement, parameter.Type ?? (SyntaxNode) parameter);
            }
        }

        var methodWalker = new MethodDependencyRelationsWalker(this, builder, methodElement, semanticModel);
        methodWalker.Visit(declaration);
    }

    private void ExtractStorageMemberDependencies(DotNetStorageTypeMember storageElement, DependencyRelations.Builder builder)
    {
        var type = storageElement.Symbol switch
        {
            IPropertySymbol property => property.Type,
            IFieldSymbol field => field.Type,
            _ => throw new Exception(
                $"Unexpected symbol type of {nameof(DotNetStorageTypeMember)}: {storageElement.Symbol.GetType().Name}.")
        };

        if (TryGetElementFromSymbol(type) is not DotNetType typeElement)
        {
            return;
        }

        builder.IsOfType.Add(storageElement, typeElement, default);
    }

    public class Builder
    {
        private readonly List<DotNetElement> _elements = new();
        private readonly BinaryRelation<DotNetElement, DotNetElement, EmptyStruct>.Builder _hierarchyBuilder = new();

        private readonly Dictionary<ISymbol, DotNetElement> _symbolsToElements = new(SymbolEqualityComparer.Default);

        private readonly List<string> _pathsToAdd = new();

        public Builder AddProject(string projectPath)
        {
            var fileExtension = Path.GetExtension(projectPath);
            if (fileExtension != ".csproj")
            {
                throw new ArgumentException(
                    $"Unsupported extension '{fileExtension}' of project file '{projectPath}'",
                    nameof(projectPath));
            }

            _pathsToAdd.Add(projectPath);

            return this;
        }

        public async Task<DotNetContext> BuildAsync()
        {
            foreach (var path in _pathsToAdd)
            {
                // We currently support only C# projects
                Debug.Assert(Path.GetExtension(path) == ".csproj");

                var project = await RoslynUtils.OpenProjectAsync(path);

                var compilation = await project.GetCompilationAsync();
                if (compilation is null)
                {
                    throw new InvalidOperationException(
                        $"The project '{path}' could not be loaded into a Roslyn Compilation.");
                }

                var projectElement = new DotNetProject(project, compilation, $"{project.AssemblyName}.dll");
                _elements.Add(projectElement);

                foreach (var member in compilation.GlobalNamespace.GetMembers())
                {
                    ProcessSymbolRecursively(projectElement, member);
                }
            }

            return new DotNetContext(
                _elements.ToImmutableArray(),
                _hierarchyBuilder.Build(),
                new Dictionary<ISymbol, DotNetElement>(_symbolsToElements, SymbolEqualityComparer.Default));
        }

        private void ProcessSymbolRecursively(DotNetElement parent, ISymbol symbol)
        {
            if (!symbol.Locations.Any(location => location.IsInSource)
                || string.IsNullOrEmpty(symbol.Name)
                || symbol.IsImplicitlyDeclared
                || (!symbol.CanBeReferencedByName && symbol.Name != ".ctor"))
            {
                return;
            }

            var id = HttpUtility.HtmlEncode($"{parent.Id}.{symbol.Name}");

            DotNetElement? element = symbol switch
            {
                INamespaceSymbol namespaceSymbol => new DotNetNamespace(namespaceSymbol, id),
                ITypeSymbol typeSymbol => new DotNetType(typeSymbol, id),
                IMethodSymbol methodSymbol => new DotNetMethod(methodSymbol, id),
                IPropertySymbol propertySymbol => new DotNetProperty(propertySymbol, id),
                IFieldSymbol fieldSymbol => new DotNetField(fieldSymbol, id),
                _ => null
            };

            if (element is null)
            {
                return;
            }

            _elements.Add(element);
            _symbolsToElements.Add(symbol, element);
            _hierarchyBuilder.Add(parent, element, default);

            if (symbol is not INamespaceOrTypeSymbol symbolWithMembers)
            {
                return;
            }

            foreach (var member in symbolWithMembers.GetMembers())
            {
                ProcessSymbolRecursively(element, member);
            }
        }
    }
}
