using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet;

public partial class DotNetContext
{
    public partial class Builder
    {
        private readonly List<DotNetElement> _elements = new();
        private readonly BinaryRelation<DotNetElement, DotNetElement, EmptyStruct>.Builder _hierarchyBuilder = new();

        private readonly Dictionary<ISymbol, DotNetElement> _symbolsToElements = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<string, DotNetProject> _moduleMetadataNamesToProjects = new();

        private readonly List<string> _pathsToAdd = new();

        public Builder AddSolution(string solutionPath)
        {
            var fileExtension = Path.GetExtension(solutionPath);
            if (fileExtension != ".sln")
            {
                throw new ArgumentException(
                    $"Invalid solution extension '{fileExtension}' of solution file '{solutionPath}'.",
                    nameof(solutionPath));
            }

            _pathsToAdd.Add(solutionPath);

            return this;
        }

        public Builder AddProject(string projectPath)
        {
            var fileExtension = Path.GetExtension(projectPath);
            if (fileExtension != ".csproj")
            {
                throw new ArgumentException(
                    $"Unsupported extension '{fileExtension}' of project file '{projectPath}'.",
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
                switch (Path.GetExtension(path))
                {
                    case ".sln":
                        await ProcessSolutionAsync(await workspace.OpenSolutionAsync(path));
                        break;

                    case ".csproj":
                        await ProcessProjectAsync(await workspace.OpenProjectAsync(path));
                        break;

                    default:
                        throw new Exception($"Unknown path '{path}'");
                }
            }

            return new DotNetContext(
                _elements.ToImmutableArray(),
                _hierarchyBuilder.Build(),
                new Dictionary<ISymbol, DotNetElement>(_symbolsToElements, SymbolEqualityComparer.Default),
                new Dictionary<string, DotNetProject>(_moduleMetadataNamesToProjects));
        }

        private async Task ProcessSolutionAsync(Solution solution)
        {
            foreach (var project in solution.Projects)
            {
                await ProcessProjectAsync(project);
            }
        }

        private async Task ProcessProjectAsync(Project project)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation is null)
            {
                throw new InvalidOperationException(
                    $"The project '{project.FilePath}' could not be loaded into a Roslyn Compilation.");
            }

            var projectElement = new DotNetProject(project, compilation, project.FilePath!);
            _elements.Add(projectElement);
            _moduleMetadataNamesToProjects.Add(compilation.SourceModule.MetadataName, projectElement);

            foreach (var member in compilation.SourceModule.GlobalNamespace.GetMembers())
            {
                ProcessSymbolsRecursively(projectElement, member, projectElement);
            }
        }

        private void ProcessSymbolsRecursively(DotNetElement parent, ISymbol symbol, DotNetProject projectElement)
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
                IMethodSymbol methodSymbol => new DotNetMethod(methodSymbol, CreateControlFlowGraph(methodSymbol), id),
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

            if (symbol is INamespaceOrTypeSymbol symbolWithMembers)
            {
                foreach (var member in symbolWithMembers.GetMembers())
                {
                    ProcessSymbolsRecursively(element, member, projectElement);
                }
            }
            else if (element is DotNetMethod methodElement)
            {
                ProcessMethodContents(methodElement);
            }

            ControlFlowGraph? CreateControlFlowGraph(IMethodSymbol methodSymbol)
            {
                var location = methodSymbol.Locations.FirstOrDefault();
                if (location is null || !location.IsInSource)
                {
                    return null;
                }

                var syntaxTree = location.SourceTree;
                var syntaxNode = syntaxTree?.GetRoot().FindNode(location.SourceSpan);
                if (syntaxTree is null || syntaxNode is null)
                {
                    return null;
                }

                var semanticModel = projectElement.Compilation.GetSemanticModel(syntaxTree);

                return ControlFlowGraph.Create(syntaxNode, semanticModel);
            }
        }

        private void ProcessMethodContents(DotNetMethod methodElement)
        {
            if (methodElement.ControlFlowGraph == null)
            {
                return;
            }

            var operationBuilder = new OperationBuilder(this, methodElement);

            foreach (var block in methodElement.ControlFlowGraph.Blocks)
            {
                foreach (var operation in block.Operations)
                {
                    operationBuilder.Visit(operation);
                }

                operationBuilder.Visit(block.BranchValue);
            }
        }
    }
}
