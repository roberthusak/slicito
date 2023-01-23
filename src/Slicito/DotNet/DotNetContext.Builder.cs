using System.Collections.Immutable;
using System.Diagnostics;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;

namespace Slicito.DotNet;

public partial class DotNetContext
{
    public partial class Builder
    {
        private readonly List<DotNetElement> _elements = new();
        private readonly Relation<DotNetElement, DotNetElement, EmptyStruct>.Builder _hierarchyBuilder = new();

        private readonly Dictionary<ISymbol, DotNetElement> _symbolsToElements = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<BasicBlock, DotNetBlock> _blocksToElements = new();
        private readonly Dictionary<IOperation, DotNetOperation> _operationsToElements = new();
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

            AssertElementIdUniqueness();

            return new DotNetContext(
                _elements.ToImmutableArray(),
                _hierarchyBuilder.Build(),
                new Dictionary<ISymbol, DotNetElement>(_symbolsToElements, SymbolEqualityComparer.Default),
                new Dictionary<BasicBlock, DotNetBlock>(_blocksToElements),
                new Dictionary<IOperation, DotNetOperation>(_operationsToElements),
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
            var variableIndex = 0;

            CreateParameterElements(methodElement.Symbol.Parameters);

            if (methodElement.ControlFlowGraph == null)
            {
                return;
            }

            ProcessRegionsRecursively(methodElement.ControlFlowGraph.Root);

            var operationBuilder = new OperationBuilder(this, methodElement);

            foreach (var block in methodElement.ControlFlowGraph.Blocks)
            {
                var blockElement = new DotNetBlock(block, $"{methodElement.Id}#block:{block.Kind}:{block.Ordinal}");

                _elements.Add(blockElement);
                _blocksToElements.Add(block, blockElement);
                _hierarchyBuilder.Add(methodElement, blockElement, default);

                foreach (var operation in block.Operations)
                {
                    operationBuilder.Visit(operation);
                }

                operationBuilder.Visit(block.BranchValue);
            }

            void ProcessRegionsRecursively(ControlFlowRegion region)
            {
                CreateLocalElements(region.Locals);

                foreach (var nestedRegion in region.NestedRegions)
                {
                    ProcessRegionsRecursively(nestedRegion);
                }
            }

            void CreateParameterElements(ImmutableArray<IParameterSymbol> parameterSymbols)
            {
                foreach (var parameterSymbol in parameterSymbols)
                {
                    if (string.IsNullOrEmpty(parameterSymbol.Name)
                        || parameterSymbol.IsImplicitlyDeclared
                        || !parameterSymbol.CanBeReferencedByName)
                    {
                        return;
                    }

                    var parameterElement = new DotNetParameter(parameterSymbol, $"{methodElement.Id}#param:{parameterSymbol.Name}:{variableIndex}");
                    variableIndex++;

                    _elements.Add(parameterElement);
                    _symbolsToElements.Add(parameterSymbol, parameterElement);
                    _hierarchyBuilder.Add(methodElement, parameterElement, default);
                }
            }

            void CreateLocalElements(ImmutableArray<ILocalSymbol> localSymbols)
            {
                foreach (var localSymbol in localSymbols)
                {
                    if (string.IsNullOrEmpty(localSymbol.Name)
                        || localSymbol.IsImplicitlyDeclared
                        || !localSymbol.CanBeReferencedByName)
                    {
                        return;
                    }

                    var localElement = new DotNetLocal(localSymbol, $"{methodElement.Id}#local:{localSymbol.Name}:{variableIndex}");
                    variableIndex++;

                    _elements.Add(localElement);
                    _symbolsToElements.Add(localSymbol, localElement);
                    _hierarchyBuilder.Add(methodElement, localElement, default);
                }
            }
        }

        [Conditional("DEBUG")]
        private void AssertElementIdUniqueness()
        {
            var idGroups = _elements.GroupBy(e => e.Id);

            foreach (var idGroup in idGroups)
            {
                Debug.Assert(idGroup.Count() == 1, $"There are {idGroup.Count()} elements with ID '{idGroup.Key}'.");
            }
        }
    }
}
