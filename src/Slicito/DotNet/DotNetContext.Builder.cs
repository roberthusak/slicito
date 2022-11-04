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

public partial class DotNetContext
{
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
            using var workspace = RoslynUtils.CreateMSBuildWorkspace();

            foreach (var path in _pathsToAdd)
            {
                // We currently support only C# projects
                Debug.Assert(Path.GetExtension(path) == ".csproj");

                var project = await workspace.OpenProjectAsync(path);

                var compilation = await project.GetCompilationAsync();
                if (compilation is null)
                {
                    throw new InvalidOperationException(
                        $"The project '{path}' could not be loaded into a Roslyn Compilation.");
                }

                var projectElement = new DotNetProject(project, compilation, path);
                _elements.Add(projectElement);

                foreach (var member in compilation.SourceModule.GlobalNamespace.GetMembers())
                {
                    ProcessSymbolRecursively(projectElement, member, projectElement);
                }
            }

            return new DotNetContext(
                _elements.ToImmutableArray(),
                _hierarchyBuilder.Build(),
                new Dictionary<ISymbol, DotNetElement>(_symbolsToElements, SymbolEqualityComparer.Default));
        }

        private void ProcessSymbolRecursively(DotNetElement parent, ISymbol symbol, DotNetProject projectElement)
        {
            if (string.IsNullOrEmpty(symbol.Name)
                || symbol.IsImplicitlyDeclared
                || (!symbol.CanBeReferencedByName && symbol.Name != ".ctor"))
            {
                return;
            }

            var id = $"{projectElement.Id}.{symbol.GetUniqueNameWithinProject()}";

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
                ProcessSymbolRecursively(element, member, projectElement);
            }
        }
    }
}
