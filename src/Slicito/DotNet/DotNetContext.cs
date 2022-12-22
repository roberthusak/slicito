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
    private readonly Dictionary<IOperation, DotNetOperation> _operationsToElements;
    private readonly Dictionary<string, DotNetProject> _moduleMetadataNamesToProjects;

    private DependencyRelations? _cachedDependencyRelations;

    public IEnumerable<DotNetElement> Elements { get; }

    public IBinaryRelation<DotNetElement, DotNetElement, EmptyStruct> Hierarchy { get; }

    private DotNetContext(
        ImmutableArray<DotNetElement> elements,
        BinaryRelation<DotNetElement, DotNetElement, EmptyStruct> hierarchy,
        Dictionary<ISymbol, DotNetElement> symbolsToElements,
        Dictionary<IOperation, DotNetOperation> operationsToElements,
        Dictionary<string, DotNetProject> moduleMetadataNamesToProjects)
    {
        Elements = elements;
        Hierarchy = hierarchy;
        _symbolsToElements = symbolsToElements;
        _operationsToElements = operationsToElements;
        _moduleMetadataNamesToProjects = moduleMetadataNamesToProjects;
    }

    public DotNetElement? TryGetElementFromSymbol(ISymbol symbol)
    {
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

    public DotNetOperation? TryGetElementFromOperation(IOperation operation) =>
        _operationsToElements.GetValueOrDefault(operation);

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
}
