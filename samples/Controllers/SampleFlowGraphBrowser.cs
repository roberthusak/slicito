using Slicito.Abstractions;
using Slicito.Abstractions.Models;
using Slicito.ProgramAnalysis.Notation;

namespace Controllers;

public class SampleFlowGraphBrowser : IController
{
    private readonly IFlowGraph _flowGraph;

    public SampleFlowGraphBrowser(IFlowGraph? flowGraph = null)
    {
        _flowGraph = flowGraph ?? FlowGraphHelper.CreateSampleFlowGraph();
    }

    public Task<IModel> InitAsync()
    {
        return Task.FromResult<IModel>(FlowGraphHelper.CreateGraphModel(_flowGraph));
    }

    public Task<IModel?> ProcessCommandAsync(Command command)
    {
        // For now, we don't handle any commands as this is a static visualization
        return Task.FromResult<IModel?>(null);
    }
} 
