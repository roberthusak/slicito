using System.Collections.Concurrent;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Slicito.Abstractions;
using Slicito.Abstractions.Interaction;
using Slicito.ProgramAnalysis.Notation;

namespace Slicito.DotNet.Implementation;
internal class SliceCreator
{
    private readonly Solution _solution;
    private readonly DotNetTypes _types;
    private readonly ISliceManager _sliceManager;

    private readonly ElementCache _elementCache;
    private readonly ConcurrentDictionary<IMethodSymbol, (IFlowGraph FlowGraph, OperationMapping OperationMapping)?> _flowGraphCache = [];
    private readonly ConcurrentDictionary<IMethodSymbol, ProcedureSignature> _procedureSignatureCache = [];

    public ILazySlice LazySlice { get; }

    public SliceCreator(Solution solution, DotNetTypes types, ISliceManager sliceManager)
    {
        _solution = solution;
        _types = types;
        _sliceManager = sliceManager;

        _elementCache = new(types);

        LazySlice = CreateSlice();
    }

    public IFlowGraph? TryCreateFlowGraph(ElementId elementId) => TryCreateFlowGraphAndMapping(elementId)?.FlowGraph;

    private (IFlowGraph FlowGraph, OperationMapping OperationMapping)? TryCreateFlowGraphAndMapping(ElementId elementId)
    {
        var element = _elementCache.TryGetSymbol(elementId);

        if (element is not IMethodSymbol method)
        {
            return null;
        }

        return _flowGraphCache.GetOrAdd(method, _ => FlowGraphCreator.TryCreate(method, _solution, _elementCache));
    }

    public ProcedureSignature GetProcedureSignature(ElementId elementId)
    {
        var method = _elementCache.GetMethod(elementId);

        return _procedureSignatureCache.GetOrAdd(method, _ => ProcedureSignatureCreator.Create(method, elementId));
    }

    public Project GetProject(ElementId elementId) => _elementCache.GetProject(elementId);

    public ISymbol GetSymbol(ElementId elementId) => _elementCache.GetSymbol(elementId);

    private ILazySlice CreateSlice()
    {
        var namespaceMemberTypes = _types.Namespace | _types.Type;
        var typeMemberTypes = _types.Type | _types.Property | _types.Field | _types.Method;

        var symbolTypes = _types.Namespace | _types.Type | _types.Property | _types.Field | _types.Method;

        return _sliceManager.CreateBuilder()
            .AddRootElements(_types.Project, LoadProjects)
            .AddHierarchyLinks(_types.Contains, _types.Project, _types.Namespace, LoadProjectNamespacesAsync)
            .AddHierarchyLinks(_types.Contains, _types.Namespace, namespaceMemberTypes, LoadNamespaceMembers)
            .AddHierarchyLinks(_types.Contains, _types.Type, typeMemberTypes, LoadTypeMembers)
            .AddHierarchyLinks(_types.Contains, _types.Method, _types.Operation, LoadMethodOperations)
            .AddLinks(_types.Calls, _types.Operation, _types.Method, LoadCallees)
            .AddElementAttribute(_types.Project, DotNetAttributeNames.Name, LoadProjectName)
            .AddElementAttribute(symbolTypes, DotNetAttributeNames.Name, LoadSymbolName)
            .AddElementAttribute(_types.Operation, DotNetAttributeNames.Name, LoadOperationName)
            .AddElementAttribute(symbolTypes, CommonAttributeNames.CodeLocation, LoadCodeLocation)
            .BuildLazy();
    }

    private IEnumerable<ISliceBuilder.PartialElementInfo> LoadProjects() =>
        _solution.Projects
            .Select(project => ToPartialElementInfo(_elementCache.GetElement(project)));

    private async ValueTask<IEnumerable<ISliceBuilder.PartialLinkInfo>> LoadProjectNamespacesAsync(ElementId sourceId)
    {
        var project = _elementCache.GetProject(sourceId);

        var compilation = await project.GetCompilationAsync()
            ?? throw new InvalidOperationException(
                $"The project '{project.FilePath}' could not be loaded into a Roslyn Compilation.");

        return compilation.SourceModule.GlobalNamespace.GetMembers()
            .OfType<INamespaceSymbol>()
            .Select(namespaceSymbol => ToPartialLinkInfo(_elementCache.GetElement(namespaceSymbol)));
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadNamespaceMembers(ElementId sourceId)
    {
        var @namespace = _elementCache.GetNamespace(sourceId);

        return @namespace.GetMembers()
            .Select(member => ToPartialLinkInfo(_elementCache.GetElement(member)));
    }

    private IEnumerable<ISliceBuilder.PartialLinkInfo> LoadTypeMembers(ElementId sourceId)
    {
        var type = _elementCache.GetType(sourceId);

        return type.GetMembers()
            .Select(member => ToPartialLinkInfo(_elementCache.GetElement(member)));
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

    private string LoadProjectName(ElementId elementId) => Path.GetFileName(elementId.Value);

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
