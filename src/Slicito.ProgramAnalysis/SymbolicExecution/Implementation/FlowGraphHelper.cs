using Slicito.ProgramAnalysis.Notation;

namespace Slicito.ProgramAnalysis.SymbolicExecution.Implementation;

internal static class FlowGraphHelper
{
    public static HashSet<BasicBlock> GetBlocksReachingTargets(
        IFlowGraph flowGraph,
        IEnumerable<BasicBlock> targetBlocks)
    {
        var result = new HashSet<BasicBlock>();
        var workList = new Queue<BasicBlock>(targetBlocks);
        
        // Add initial target blocks to result
        foreach (var block in targetBlocks)
        {
            result.Add(block);
        }

        // Process blocks in work list until empty
        while (workList.Count > 0)
        {
            var current = workList.Dequeue();
            
            // Get all predecessors of current block
            foreach (var predecessor in flowGraph.GetPredecessors(current))
            {
                // If we haven't processed this predecessor yet
                if (result.Add(predecessor))
                {
                    // Add it to the work list for processing
                    workList.Enqueue(predecessor);
                }
            }
        }

        return result;
    }

    public static Dictionary<BasicBlock, int> GetBlockTopologicalOrder(IFlowGraph flowGraph, HashSet<BasicBlock> consideredBlocks)
    {
        var result = new Dictionary<BasicBlock, int>();
        var inDegree = new Dictionary<BasicBlock, int>();
        var queue = new Queue<BasicBlock>();

        // Calculate in-degree for each block and find starting nodes
        foreach (var block in consideredBlocks)
        {
            var predecessors = flowGraph.GetPredecessors(block).Count(p => consideredBlocks.Contains(p));
            inDegree[block] = predecessors;
            
            if (predecessors == 0)
            {
                queue.Enqueue(block);
            }
        }

        var order = 0;
        var visitedCount = 0;

        // Process nodes in topological order
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result[current] = order++;
            visitedCount++;

            foreach (var successor in flowGraph.GetSuccessors(current))
            {
                if (!consideredBlocks.Contains(successor))
                {
                    continue;
                }

                inDegree[successor]--;
                if (inDegree[successor] == 0)
                {
                    queue.Enqueue(successor);
                }
            }
        }

        // If we couldn't visit all nodes, there must be a cycle
        if (visitedCount != consideredBlocks.Count)
        {
            throw new InvalidOperationException("The flow graph contains a cycle");
        }

        return result;
    }
}
