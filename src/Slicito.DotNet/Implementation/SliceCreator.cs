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
    private readonly ConcurrentDictionary<IMethodSymbol, (IFlowGraph FlowGraph, OperationMapping OperationMapping)?> _flowGraphCache = [];
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

    public IFlowGraph? TryCreateFlowGraph(ElementId elementId) => TryCreateFlowGraphAndMapping(elementId)?.FlowGraph;

    private (IFlowGraph FlowGraph, OperationMapping OperationMapping)? TryCreateFlowGraphAndMapping(ElementId elementId)
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

        var symbolTypes = _types.Namespace | _types.Type | _types.Property | _types.Field | _types.Method;

        return _sliceManager.CreateBuilder()
            .AddRootElements(_types.Solution, LoadSolutions)
            .AddHierarchyLinks(_types.Contains, _types.Solution, _types.Project, LoadSolutionProjects)
            .AddHierarchyLinks(_types.Contains, _types.Project, _types.Namespace, LoadProjectNamespacesAsync)
            .AddHierarchyLinks(_types.Contains, _types.Namespace, namespaceMemberTypes, LoadNamespaceMembersAsync)
            .AddHierarchyLinks(_types.Contains, _types.Type, typeMemberTypes, LoadTypeMembersAsync)
            .AddHierarchyLinks(_types.Contains, _types.Method, _types.Operation, LoadMethodOperations)
            .AddLinks(_types.References, _types.Project, _types.Project, LoadProjectReferences)
            .AddLinks(_types.Calls, _types.Operation, _types.Method, LoadCallees)
            .AddElementAttribute(_types.Solution, DotNetAttributeNames.Name, LoadSolutionName)
            .AddElementAttribute(_types.Project, DotNetAttributeNames.Name, LoadProjectName)
            .AddElementAttribute(_types.Project, DotNetAttributeNames.OutputKind, LoadProjectOutputKind)
            .AddElementAttribute(symbolTypes, DotNetAttributeNames.Name, LoadSymbolName)
            .AddElementAttribute(_types.Operation, DotNetAttributeNames.Name, LoadOperationName)
            .AddElementAttribute(symbolTypes, CommonAttributeNames.CodeLocation, LoadCodeLocation)
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
        var @namespace = _elementCache.GetNamespaceAndProject(sourceId, out var project);

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
        var type = _elementCache.GetTypeAndProject(sourceId, out var project);

        return await Task.WhenAll(
            type.GetMembers()
                .Select(async member =>
                {
                    var element = await _elementCache.GetElementAsync(project, member);
                    return ToPartialLinkInfo(element);
                }));
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadMethodOperations(ElementId sourceId)
    {
        var result = TryCreateFlowGraphAndMapping(sourceId);
        if (result is null)
        {
            yield break;
        }

        var (flowGraph, mapping) = result.Value;

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

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadCallees(ElementId sourceId)
    {
        var methodId = ElementIdProvider.GetMethodIdFromOperationId(sourceId);

        var method = _elementCache.GetMethod(methodId);

        var operation = _flowGraphCache[method]!.Value.OperationMapping.GetOperation(sourceId);

        if (operation is Operation.Call call)
        {
            var calleeId = new ElementId(call.Signature.Name);

            yield return ToPartialLinkInfo(new(calleeId, _types.Method));
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
        var methodId = ElementIdProvider.GetMethodIdFromOperationId(elementId);

        var method = _elementCache.GetMethod(methodId);

        var syntax = _flowGraphCache[method]!.Value.OperationMapping.GetSyntax(elementId);

        return syntax.ToString();
    }

    private string LoadCodeLocation(ElementId elementId)
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

        var lineSpan = syntax?.SyntaxTree.GetLocation(span.Value).GetLineSpan();
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
