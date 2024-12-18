using Microsoft.VisualStudio.LanguageServices;

using Slicito.Abstractions;
using Slicito.Abstractions.Queries;
using Slicito.Common;
using Slicito.Common.Extensibility;
using Slicito.DotNet;
using Slicito.ProgramAnalysis;

namespace Slicito.VisualStudio.Implementation;

internal class VisualStudioSlicitoContext : ProgramAnalysisContextBase
{
    private readonly VisualStudioWorkspace _workspace;
    private DotNetSolutionContext _lastDotNetSolutionContext;

    private VisualStudioSlicitoContext(
        ITypeSystem typeSystem,
        ISliceManager sliceManager,
        DotNetTypes dotNetTypes,
        VisualStudioWorkspace workspace) : base(typeSystem, sliceManager, dotNetTypes)
    {
        _workspace = workspace;
        _lastDotNetSolutionContext = new DotNetSolutionContext(workspace.CurrentSolution, dotNetTypes, sliceManager);
    }

    public static VisualStudioSlicitoContext Create(VisualStudioWorkspace workspace)
    {
        var typeSystem = new TypeSystem();
        var sliceManager = new SliceManager();

        var dotNetTypes = new DotNetTypes(typeSystem);

        return new VisualStudioSlicitoContext(typeSystem, sliceManager, dotNetTypes, workspace);
    }

    public override ILazySlice WholeSlice => GetCurrentDotNetSolutionContext().LazySlice;

    public override IFlowGraphProvider FlowGraphProvider => GetCurrentDotNetSolutionContext();

    private DotNetSolutionContext GetCurrentDotNetSolutionContext()
    {
        var currentSolution = _workspace.CurrentSolution;
        if (currentSolution != _lastDotNetSolutionContext.Solution)
        {
            _lastDotNetSolutionContext = new DotNetSolutionContext(currentSolution, (DotNetTypes)ProgramTypes, SliceManager);
        }

        return _lastDotNetSolutionContext;
    }
}
