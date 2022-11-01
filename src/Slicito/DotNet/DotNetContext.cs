using System.Collections.Immutable;
using System.Diagnostics;
using System.Web;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;
using Slicito.DotNet.Relations;

namespace Slicito.DotNet;

public class DotNetContext : IContext<DotNetElement, EmptyStruct>
{
    public IEnumerable<DotNetElement> Elements { get; }

    public IBinaryRelation<DotNetElement, DotNetElement, EmptyStruct> Hierarchy { get; }

    private DotNetContext(
        ImmutableArray<DotNetElement> elements,
        BinaryRelation<DotNetElement, DotNetElement, EmptyStruct> hierarchy)
    {
        Elements = elements;
        Hierarchy = hierarchy;
    }

    public InterproceduralRelations ExtractInterproceduralRelations(Predicate<DotNetElement>? filter = null)
    {
        throw new NotImplementedException();
    }

    public class Builder
    {
        private readonly List<DotNetElement> _elements = new();
        private readonly BinaryRelation<DotNetElement, DotNetElement, EmptyStruct>.Builder _hierarchyBuilder = new();

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

            return new DotNetContext(_elements.ToImmutableArray(), _hierarchyBuilder.Build());
        }

        private void ProcessSymbolRecursively(DotNetElement parent, ISymbol symbol)
        {
            if (!symbol.Locations.Any(location => location.IsInSource)
                || string.IsNullOrEmpty(symbol.Name)
                || !symbol.CanBeReferencedByName)
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
