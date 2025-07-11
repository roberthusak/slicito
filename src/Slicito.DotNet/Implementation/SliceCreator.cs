using System.Collections.Concurrent;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Slicito.Abstractions;
using Slicito.Abstractions.Interaction;
using Slicito.DotNet.Facts;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.DotNet.Implementation;
internal class SliceCreator
{
    private readonly ImmutableArray<Solution> _solutions;
    private readonly DotNetTypes _types;
    private readonly ISliceManager _sliceManager;

    private readonly ElementCache _elementCache;
    private readonly ConcurrentDictionary<IMethodSymbol, (FlowGraphGroup FlowGraphGroup, OperationMapping OperationMapping)?> _flowGraphCache = [];
    private readonly ConcurrentDictionary<IMethodSymbol, ProcedureSignature> _procedureSignatureCache = [];

    public ISlice Slice { get; }

    public IDotNetSliceFragment TypedSliceFragment { get; }

    public SliceCreator(ImmutableArray<Solution> solutions, DotNetTypes types, ISliceManager sliceManager)
    {
        _solutions = solutions;
        _types = types;
        _sliceManager = sliceManager;

        _elementCache = new(types);

        Slice = CreateSlice();
        TypedSliceFragment = new DotNetSliceFragment(Slice, _types);
    }

    public IFlowGraph? TryCreateFlowGraph(ElementId elementId) => TryCreateFlowGraphsAndMapping(elementId)?.FlowGraphGroup.RootFlowGraph;

    private (FlowGraphGroup FlowGraphGroup, OperationMapping OperationMapping)? TryCreateFlowGraphsAndMapping(ElementId elementId)
    {
        var symbol = _elementCache.TryGetSymbol(elementId);
        var project = _elementCache.TryGetProject(elementId);

        if (symbol is not IMethodSymbol method || project is null)
        {
            return null;
        }

        return _flowGraphCache.GetOrAdd(method, _ => FlowGraphCreator.TryCreate(method, project, _elementCache));
    }

    public ProcedureSignature GetProcedureSignature(ElementId elementId)
    {
        var method = _elementCache.GetMethod(elementId);

        return _procedureSignatureCache.GetOrAdd(method, _ => ProcedureSignatureCreator.Create(method, elementId));
    }

    public Project GetProject(ElementId elementId) => _elementCache.GetProject(elementId);

    public ISymbol GetSymbol(ElementId elementId) => _elementCache.GetSymbol(elementId);

    private ISlice CreateSlice()
    {
        var namespaceMemberTypes = _types.Namespace | _types.Type;
        var typeMemberTypes = _types.Type | _types.Property | _types.Field | _types.Method;

        var namedSymbolTypes = _types.Namespace | _types.Type | _types.Property | _types.Field | _types.Method | _types.LocalFunction;
        var symbolTypes = namedSymbolTypes | _types.Lambda;

        return _sliceManager.CreateBuilder()
            .AddRootElements(_types.Solution, LoadSolutions)
            .AddHierarchyLinks(_types.Contains, _types.Solution, _types.Project, LoadSolutionProjects)
            .AddHierarchyLinks(_types.Contains, _types.Project, _types.Namespace, LoadProjectNamespacesAsync)
            .AddHierarchyLinks(_types.Contains, _types.Namespace, namespaceMemberTypes, LoadNamespaceMembersAsync)
            .AddHierarchyLinks(_types.Contains, _types.Type, typeMemberTypes, LoadTypeMembersAsync)
            .AddHierarchyLinks(_types.Contains, _types.Method, _types.LocalFunction, LoadMethodLocalFunctions)
            .AddHierarchyLinks(_types.Contains, _types.Method, _types.Lambda, LoadMethodLambdas)
            .AddHierarchyLinks(_types.Contains, _types.Method, _types.Operation, LoadMethodOperations)
            .AddHierarchyLinks(_types.Contains, _types.NestedProcedures, _types.Operation, LoadNestedProcedureOperations)
            .AddLinks(_types.References, _types.Project, _types.Project, LoadProjectReferences)
            .AddLinks(_types.Overrides, _types.Method, _types.Method, LoadMethodOverridesAsync)
            .AddLinks(_types.Calls, _types.Operation, _types.Method, LoadCallees)
            .AddElementAttribute(_types.Solution, DotNetAttributeNames.Name, LoadSolutionName)
            .AddElementAttribute(_types.Project, DotNetAttributeNames.Name, LoadProjectName)
            .AddElementAttribute(_types.Project, DotNetAttributeNames.OutputKind, LoadProjectOutputKind)
            .AddElementAttribute(namedSymbolTypes, DotNetAttributeNames.Name, LoadSymbolName)
            .AddElementAttribute(_types.Operation, DotNetAttributeNames.Name, LoadOperationName)
            .AddElementAttribute(symbolTypes, CommonAttributeNames.CodeLocation, LoadSymbolCodeLocation)
            .AddElementAttribute(_types.Operation, CommonAttributeNames.CodeLocation, LoadOperationCodeLocation)
            .Build();
    }

    private IEnumerable<ISliceBuilder.PartialElementInfo> LoadSolutions() =>
        _solutions
            .Select(solution => ToPartialElementInfo(_elementCache.GetElement(solution)));

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadSolutionProjects(ElementId sourceId)
    {
        var solution = _elementCache.GetSolution(sourceId);

        return solution.Projects
            .Select(project => ToPartialLinkInfo(_elementCache.GetElement(project)));
    }

    private async ValueTask<IEnumerable<ISliceBuilder.PartialLinkInfo>> LoadProjectNamespacesAsync(ElementId sourceId)
    {
        var project = _elementCache.GetProject(sourceId);

        var compilation = await project.GetCompilationAsync()
            ?? throw new InvalidOperationException(
                $"The project '{project.FilePath}' could not be loaded into a Roslyn Compilation.");

        if (compilation.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            throw new InvalidOperationException(
                $"The project '{project.FilePath}' has compilation errors and cannot be analyzed.");
        }

        var module = compilation.SourceModule;

        return await Task.WhenAll(
            module.GlobalNamespace.GetMembers()
                .OfType<INamespaceSymbol>()
                .Select(async namespaceSymbol =>
                {
                    var element = await _elementCache.GetElementAsync(project, namespaceSymbol);
                    return ToPartialLinkInfo(element);
                }));
    }

    private async ValueTask<IEnumerable<ISliceBuilder.PartialLinkInfo>> LoadNamespaceMembersAsync(ElementId sourceId)
    {
        var @namespace = _elementCache.GetNamespaceAndRelatedProject(sourceId, out var project);

        return await Task.WhenAll(
            @namespace.GetMembers()
                .Select(async member =>
                {
                    var element = await _elementCache.GetElementAsync(project, member);
                    return ToPartialLinkInfo(element);
                }));
    }

    private async ValueTask<IEnumerable<ISliceBuilder.PartialLinkInfo>> LoadTypeMembersAsync(ElementId sourceId)
    {
        var type = _elementCache.GetTypeAndRelatedProject(sourceId, out var project);

        return await Task.WhenAll(
            type.GetMembers()
                .Select(async member =>
                {
                    var element = await _elementCache.GetElementAsync(project, member);
                    return ToPartialLinkInfo(element);
                }));
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadMethodLocalFunctions(ElementId sourceId)
    {
        return LoadMethodNestedProcedures(sourceId, MethodKind.LocalFunction);
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadMethodLambdas(ElementId sourceId)
    {
        return LoadMethodNestedProcedures(sourceId, MethodKind.AnonymousFunction);
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadMethodNestedProcedures(ElementId sourceId, MethodKind methodKind)
    {
        var result = TryCreateFlowGraphsAndMapping(sourceId);
        if (result is null)
        {
            yield break;
        }

        var flowGraphGroup = result.Value.FlowGraphGroup;

        foreach (var nestedId in flowGraphGroup.ElementIdToNestedFlowGraph.Keys)
        {
            var nestedSymbol = _elementCache.GetMethod(nestedId);

            if (nestedSymbol.MethodKind == methodKind)
            {
                yield return ToPartialLinkInfo(new(nestedId, _types.LocalFunction));
            }
        }
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadMethodOperations(ElementId sourceId)
    {
        var result = TryCreateFlowGraphsAndMapping(sourceId);
        if (result is null)
        {
            return [];
        }

        var (flowGraphGroup, mapping) = result.Value;

        return LoadFlowGraphOperations(flowGraphGroup.RootFlowGraph, mapping);
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadNestedProcedureOperations(ElementId sourceId)
    {
        var method = _elementCache.GetMethodAndRelatedProject(sourceId, out var project);

        var rootMethod = RoslynHelper.GetContainingMethodOrSelf(method);

        var rootMethodIdTask = _elementCache.GetElementAsync(project, rootMethod);
        if (!rootMethodIdTask.IsCompleted)
        {
            throw new InvalidOperationException(
                $"The containing method '{rootMethod.Name}' of '{method.Name}' was unexpectedly not present in the cache and was restored asynchronously.");
        }

        var rootMethodId = rootMethodIdTask.Result.Id;

        var result = TryCreateFlowGraphsAndMapping(rootMethodId);
        if (result is null)
        {
            return [];
        }

        var (flowGraphGroup, mapping) = result.Value;

        return LoadFlowGraphOperations(flowGraphGroup.ElementIdToNestedFlowGraph[sourceId], mapping);
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadFlowGraphOperations(IFlowGraph flowGraph, OperationMapping mapping)
    {
        foreach (var block in flowGraph.Blocks.OfType<BasicBlock.Inner>())
        {
            if (block.Operation is null)
            {
                continue;
            }

            var operationType = block.Operation switch
            {
                Operation.Assignment => _types.Assignment,
                Operation.Call => _types.Call,
                Operation.ConditionalJump => _types.ConditionalJump,
                _ => throw new ArgumentException($"Unsupported operation: {block.Operation}"),
            };

            yield return ToPartialLinkInfo(new(mapping.GetId(block.Operation), operationType));
        }
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadProjectReferences(ElementId sourceId)
    {
        var project = _elementCache.GetProject(sourceId);

        foreach (var reference in project.ProjectReferences)
        {
            var referencedProject = project.Solution.GetProject(reference.ProjectId)
                ?? throw new InvalidOperationException(
                    $"Project '{project.FilePath}' references project '{reference.ProjectId}' which could not be found in the solution.");

            yield return ToPartialLinkInfo(_elementCache.GetElement(referencedProject));
        }
    }

    private async ValueTask<IEnumerable<ISliceBuilder.PartialLinkInfo>> LoadMethodOverridesAsync(ElementId sourceId)
    {
        var method = _elementCache.GetMethodAndRelatedProject(sourceId, out var project);

        if (method.ContainingType is not ITypeSymbol type)
        {
            return [];
        }

        var interfaceMemberQuery =
            from iface in type.AllInterfaces
            from interfaceMember in iface.GetMembers()
            let impl = type.FindImplementationForInterfaceMember(interfaceMember)
            where method.Equals(impl, SymbolEqualityComparer.Default)
            select interfaceMember;

        var interfaceMembers = await Task.WhenAll(interfaceMemberQuery.Select(async interfaceMember =>
        {
            var element = await _elementCache.GetElementAsync(project, interfaceMember);
            return ToPartialLinkInfo(element);
        }));

        var result = interfaceMembers.ToList();

        if (method.OverriddenMethod is not null)
        {
            var overriddenMethodElement = await _elementCache.GetElementAsync(project, method.OverriddenMethod);

            result.Add(ToPartialLinkInfo(overriddenMethodElement));
        }

        return result;
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadCallees(ElementId sourceId)
    {
        var methodId = ElementIdProvider.GetMethodIdFromOperationId(sourceId);

        var method = _elementCache.GetMethod(methodId);

        var operationMapping = _flowGraphCache[method]!.Value.OperationMapping;

        var operation = operationMapping.GetOperation(sourceId);
        if (operation is Operation.Call call)
        {
            var calleeId = new ElementId(call.Signature.Name);

            yield return ToPartialLinkInfo(new(calleeId, _types.Method));
        }

        if (operationMapping.TryGetAdditionalLinks(sourceId, out var additionalLinks))
        {
            foreach (var callTarget in additionalLinks.CallTargets)
            {
                yield return ToPartialLinkInfo(callTarget);
            }
        }
    }

    private string LoadSolutionName(ElementId elementId) => Path.GetFileName(elementId.Value);

    private string LoadProjectName(ElementId elementId) => Path.GetFileName(elementId.Value);

    private string LoadProjectOutputKind(ElementId elementId)
    {
        var project = _elementCache.GetProject(elementId);

        var roslynOutputKind = project.CompilationOptions?.OutputKind
            ?? throw new InvalidOperationException($"Project '{project.FilePath}' has no compilation options.");

        var outputKind = roslynOutputKind switch
        {
            OutputKind.ConsoleApplication or
            OutputKind.WindowsApplication or
            OutputKind.WindowsRuntimeApplication => ProjectOutputKind.Executable,

            OutputKind.DynamicallyLinkedLibrary or
            OutputKind.NetModule or
            OutputKind.WindowsRuntimeMetadata => ProjectOutputKind.Library,

            _ => throw new InvalidOperationException($"Unsupported project output kind: {roslynOutputKind}"),
        };

        return outputKind.ToString();
    }

    private string LoadSymbolName(ElementId elementId) =>
        _elementCache.GetSymbol(elementId).Name;

    private string LoadOperationName(ElementId elementId)
    {
        var syntax = GetOperationSyntax(elementId);

        return syntax.ToString();
    }

    private string LoadSymbolCodeLocation(ElementId elementId)
    {
        var symbol = _elementCache.GetSymbol(elementId);

        var syntax = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

        var span = syntax switch
        {
            MethodDeclarationSyntax methodDeclaration => methodDeclaration.Identifier.Span,
            _ => syntax?.Span
        };

        if (span is null)
        {
            return "";
        }

        return ConvertSyntaxToCodeLocation(syntax, span.Value);
    }

    private string LoadOperationCodeLocation(ElementId elementId)
    {
        var syntax = GetOperationSyntax(elementId);

        return ConvertSyntaxToCodeLocation(syntax, syntax.Span);
    }

    private SyntaxNode GetOperationSyntax(ElementId elementId)
    {
        var methodId = ElementIdProvider.GetMethodIdFromOperationId(elementId);

        var method = _elementCache.GetMethod(methodId);

        return _flowGraphCache[method]!.Value.OperationMapping.GetSyntax(elementId);
    }

    private static string ConvertSyntaxToCodeLocation(SyntaxNode? syntax, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        var lineSpan = syntax?.SyntaxTree.GetLocation(span).GetLineSpan();
        if (lineSpan is null)
        {
            return "";
        }

        var codeLocation = new CodeLocation(
            lineSpan.Value.Path,
            lineSpan.Value.StartLinePosition.Line + 1,
            lineSpan.Value.StartLinePosition.Character);

        return codeLocation.Format();
    }

    private static ISliceBuilder.PartialElementInfo ToPartialElementInfo(ElementInfo element) =>
        new(element.Id, element.Type);

    private static ISliceBuilder.PartialLinkInfo ToPartialLinkInfo(ElementInfo target) =>
        new(new(target.Id, target.Type));
}
