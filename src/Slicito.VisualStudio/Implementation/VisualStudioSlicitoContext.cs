using System.IO;

using Microsoft.VisualStudio.LanguageServices;

using Slicito.Abstractions;
using Slicito.Abstractions.Facts;
using Slicito.Abstractions.Interaction;
using Slicito.Common;
using Slicito.Common.Extensibility;
using Slicito.DotNet;
using Slicito.ProgramAnalysis;
using Slicito.ProgramAnalysis.SymbolicExecution.SmtSolver;

namespace Slicito.VisualStudio.Implementation;

internal class VisualStudioSlicitoContext : ProgramAnalysisContextBase
{
    private readonly VisualStudioWorkspace _workspace;
    private DotNetSolutionContext _lastDotNetSolutionContext;

    private VisualStudioSlicitoContext(
        ITypeSystem typeSystem,
        ISliceManager sliceManager,
        DotNetTypes dotNetTypes,
        ICodeNavigator codeNavigator,
        ISolverFactory solverFactory,
        VisualStudioWorkspace workspace) : base(typeSystem, sliceManager, dotNetTypes)
    {
        _workspace = workspace;
        _lastDotNetSolutionContext = new DotNetSolutionContext(workspace.CurrentSolution, dotNetTypes, sliceManager);

        SetService(codeNavigator);
        SetService(solverFactory);
    }

    public static VisualStudioSlicitoContext Create(VisualStudioWorkspace workspace)
    {
        var typeSystem = new TypeSystem();
        var sliceManager = new SliceManager(typeSystem);

        var dotNetTypes = new DotNetTypes(typeSystem);

        var codeNavigator = new VisualStudioCodeNavigator();

        var assemblyPath = Path.GetDirectoryName(typeof(VisualStudioSlicitoContext).Assembly.Location);
        var z3Path = Path.Combine(assemblyPath, "z3.exe");
        var solverFactory = new SmtLibCliSolverFactory(z3Path, ["-in"]);

        return new VisualStudioSlicitoContext(typeSystem, sliceManager, dotNetTypes, codeNavigator, solverFactory, workspace);
    }

    public override ISlice WholeSlice => GetCurrentDotNetSolutionContext().Slice;

    public override IFlowGraphProvider FlowGraphProvider => GetCurrentDotNetSolutionContext();

    public new DotNetTypes ProgramTypes => (DotNetTypes) base.ProgramTypes;

    private DotNetSolutionContext GetCurrentDotNetSolutionContext()
    {
        var currentSolution = _workspace.CurrentSolution;
        if (currentSolution != _lastDotNetSolutionContext.Solution)
        {
            _lastDotNetSolutionContext = new DotNetSolutionContext(currentSolution, ProgramTypes, SliceManager);
        }

        return _lastDotNetSolutionContext;
    }
}
