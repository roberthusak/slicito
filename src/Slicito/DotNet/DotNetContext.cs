using System.Collections.Immutable;
using System.Diagnostics;
using System.Web;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;
using Slicito.DotNet.Relations;

namespace Slicito.DotNet;

public partial class DotNetContext : IContext
{
    private readonly Dictionary<ISymbol, DotNetElement> _symbolsToElements;
    private readonly Dictionary<BasicBlock, DotNetBlock> _blocksToElements;
    private readonly Dictionary<IOperation, DotNetOperation> _operationsToElements;
    private readonly Dictionary<string, DotNetProject> _moduleMetadataNamesToProjects;

    private DependencyRelations? _cachedDependencyRelations;

    public IEnumerable<DotNetElement> Elements { get; }

    public IRelation<DotNetElement, DotNetElement, EmptyStruct> Hierarchy { get; }

    IEnumerable<IElement> IContext.Elements => Elements;

    IRelation<IElement, IElement, EmptyStruct> IContext.Hierarchy => Hierarchy;

    private DotNetContext(
        ImmutableArray<DotNetElement> elements,
        Relation<DotNetElement, DotNetElement, EmptyStruct> hierarchy,
        Dictionary<ISymbol, DotNetElement> symbolsToElements,
        Dictionary<BasicBlock, DotNetBlock> blocksToElements,
        Dictionary<IOperation, DotNetOperation> operationsToElements,
        Dictionary<string, DotNetProject> moduleMetadataNamesToProjects)
    {
        Elements = elements;
        Hierarchy = hierarchy;
        _symbolsToElements = symbolsToElements;
        _blocksToElements = blocksToElements;
        _operationsToElements = operationsToElements;
        _moduleMetadataNamesToProjects = moduleMetadataNamesToProjects;
    }

    public static Task<DotNetContext> CreateFromProjectAsync(string projectPath) =>
        new Builder()
        .AddProject(projectPath)
        .BuildAsync();

    public static Task<DotNetContext> CreateFromSolutionAsync(string solutionPath) =>
        new Builder()
        .AddSolution(solutionPath)
        .BuildAsync();

    public DotNetElement? TryGetElementFromSymbol(ISymbol? symbol)
    {
        if (symbol is null)
        {
            return null;
        }

        var element = _symbolsToElements.GetValueOrDefault(symbol);

        if (element is null
            && symbol.Name != ""
            && symbol.DeclaringSyntaxReferences.Length > 0
            && _moduleMetadataNamesToProjects.TryGetValue(symbol.ContainingModule.MetadataName, out var projectElement))
        {
            // Reload the proper source symbol, since certain symbols might as as proxies ("retargeting"), without the equality working

            var symbolUniqueName = symbol.GetUniqueNameWithinProject();

            var reloadedSymbol = projectElement.Compilation
                .GetSymbolsWithName(symbol.Name)
                .Where(s => s.GetUniqueNameWithinProject() == symbolUniqueName)
                .SingleOrDefault();

            if (reloadedSymbol is not null)
            {
                element = _symbolsToElements.GetValueOrDefault(reloadedSymbol);
            }
        }

        return element;
    }

    public DotNetBlock? TryGetElementFromBlock(BasicBlock? block) =>
        block is null
        ? null
        : _blocksToElements.GetValueOrDefault(block);

    public DotNetOperation? TryGetElementFromOperation(IOperation? operation) =>
        operation is null
        ? null
        : _operationsToElements.GetValueOrDefault(operation);

    public DependencyRelations ExtractDependencyRelations(Predicate<DotNetElement>? filter = null)
    {
        if (filter == null)
        {
            _cachedDependencyRelations ??= Compute();

            return _cachedDependencyRelations;
        }
        else
        {
            return Compute(); 
        }

        DependencyRelations Compute()
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
    }

    public DataFlowRelations ExtractDataFlowRelations(Predicate<DotNetElement>? filter = null)
    {
        var dependencyRelations = ExtractDependencyRelations(filter);

        var builder = new DataFlowRelations.Builder(dependencyRelations);

        var operationVisitor = new OperationDataFlowRelationsVisitor(this, builder);

        foreach (var element in Elements)
        {
            if ((filter is not null && !filter(element))
                || element is not DotNetOperation operationElement)
            {
                continue;
            }

            operationVisitor.Visit(operationElement.Operation, operationElement);
        }

        return builder.Build();
    }

    public ControlFlowRelations ExtractControlFlowRelations(Predicate<DotNetMethod>? filter = null)
    {
        var dependencyRelations = ExtractDependencyRelations();

        var builder = new ControlFlowRelations.Builder(dependencyRelations);

        foreach (var method in Elements.OfType<DotNetMethod>())
        {
            if (filter is not null && !filter(method))
            {
                continue;
            }

            OperationControlFlowRelationsWalker.VisitMethod(this, method, builder);
        }

        ExtractInterproceduralControlFlowRelations(builder);

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

        foreach (var implementedInterfaceMethod in methodSymbol.FindExplicitOrImplicitInterfaceImplementations())
        {
            if (TryGetElementFromSymbol(implementedInterfaceMethod) is DotNetMethod implementedInterfaceMethodElement)
            {
                builder.Overrides.Add(methodElement, implementedInterfaceMethodElement, default);
            }
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

        var operationVisitor = new OperationDependencyRelationsVisitor(this, builder);

        var operationsQuery =
            (from pair in Hierarchy.GetOutgoing(methodElement)
             select pair.Target)
            .OfType<DotNetOperation>();

        foreach (var operationElement in operationsQuery)
        {
            operationVisitor.Visit(operationElement.Operation, operationElement);
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

    private void ExtractInterproceduralControlFlowRelations(ControlFlowRelations.Builder builder)
    {
        var directCalls = builder.DependencyRelations.Calls.SetData(new EmptyStruct());

        var calls = Relation.Merge(
            directCalls,
            directCalls.Join(
                builder.DependencyRelations.Overrides
                    .Invert()
                    .CreateTransitiveClosure()));

        foreach (var callPair in calls.Pairs)
        {
            var invocationElement = callPair.Source;
            var methodElement = callPair.Target;

            var entryBlockElement = TryGetElementFromBlock(methodElement.ControlFlowGraph?.Blocks[0]);
            var exitBlockElement = TryGetElementFromBlock(methodElement.ControlFlowGraph?.Blocks[^1]);

            if (entryBlockElement is not null)
            {
                foreach (var preInvocationPair in builder.IsSucceededByWithLeftOutInvocation.AsRelation().GetIncoming(invocationElement))
                {
                    builder.IsSucceededByWithInvocation.Add(preInvocationPair.Source, entryBlockElement, default);
                }
            }

            if (exitBlockElement is not null)
            {
                builder.IsSucceededByWithReturn.Add(exitBlockElement, invocationElement, default);
            }
        }
    }
}
