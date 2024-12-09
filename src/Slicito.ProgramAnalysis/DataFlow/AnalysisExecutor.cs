using System.Diagnostics.CodeAnalysis;

using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis.DataFlow;

public static class AnalysisExecutor
{
    public static AnalysisResult<TDomain> Execute<TDomain>(
        IFlowGraph graph,
        IDataFlowAnalysis<TDomain> analysis)
    {
        return new Executor<TDomain>(graph, analysis).Execute();
    }

    private class Executor<TDomain>(IFlowGraph graph, IDataFlowAnalysis<TDomain> analysis)
    {
        private readonly Queue<BasicBlock> _worklist = new();
        private readonly HashSet<BasicBlock> _inWorklist = new();

        public AnalysisResult<TDomain> Execute()
        {
            analysis.Initialize(graph);

            var inputMap = graph.Blocks.ToDictionary(block => block, analysis.GetInitialInputValue);
            var outputMap = graph.Blocks.ToDictionary(block => block, analysis.GetInitialOutputValue);

            if (analysis.Direction == AnalysisDirection.Forward)
            {
                ExecuteForward(inputMap, outputMap);
            }
            else
            {
                ExecuteBackward(inputMap, outputMap);
            }

            return new AnalysisResult<TDomain>(inputMap, outputMap);
        }

        private void ExecuteForward(Dictionary<BasicBlock, TDomain> inputMap, Dictionary<BasicBlock, TDomain> outputMap)
        {
            foreach (var block in graph.Blocks)
            {
                Enqueue(block);
            }

            while (TryDequeue(out var block))
            {
                var predecessors = graph.GetPredecessors(block);

                if (predecessors.Any())
                {
                    inputMap[block] = predecessors
                        .Select(pred => outputMap[pred])
                        .Aggregate(analysis.Meet);
                }

                var output = analysis.Transfer(block, inputMap[block]);

                if (!analysis.Equals(output, outputMap[block]))
                {
                    foreach (var successor in graph.GetSuccessors(block))
                    {
                        Enqueue(successor);
                    }
                }

                outputMap[block] = output;
            }
        }

        private void ExecuteBackward(Dictionary<BasicBlock, TDomain> inputMap, Dictionary<BasicBlock, TDomain> outputMap)
        {
            foreach (var block in graph.Blocks.Reverse())
            {
                Enqueue(block);
            }

            while (TryDequeue(out var block))
            {
                var successors = graph.GetSuccessors(block);

                if (successors.Any())
                {
                    outputMap[block] = successors
                        .Select(succ => inputMap[succ])
                        .Aggregate(analysis.Meet);
                }

                var input = analysis.Transfer(block, outputMap[block]);

                if (!analysis.Equals(input, inputMap[block]))
                {
                    foreach (var predecessor in graph.GetPredecessors(block))
                    {
                        Enqueue(predecessor);
                    }
                }

                inputMap[block] = input;
            }
        }

        private void Enqueue(BasicBlock block)
        {
            if (_inWorklist.Add(block))
            {
                _worklist.Enqueue(block);
            }
        }

        private bool TryDequeue([NotNullWhen(true)] out BasicBlock? block)
        {
            if (_worklist.Count > 0)
            {
                block = _worklist.Dequeue();
                _inWorklist.Remove(block);
                return true;
            }

            block = null;
            return false;
        }
    }
}
