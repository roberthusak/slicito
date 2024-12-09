using Slicito.ProgramAnalysis.SymbolicExecution.SmtLib;

namespace Slicito.ProgramAnalysis.SymbolicExecution.Implementation;

internal sealed class ImmutableConditionStack
{
    private readonly ImmutableConditionStack? _tail;

    public ImmutableConditionStack(Term condition) : this(condition, null, 1) { }

    private ImmutableConditionStack(Term condition, ImmutableConditionStack? tail, int size)
    {
        Condition = condition;
        _tail = tail;
        Size = size;
    }

    public Term Condition { get; }

    public int Size { get; }

    public ImmutableConditionStack Push(Term condition) => new ImmutableConditionStack(condition, this, Size + 1);

    public ImmutableConditionStack? Pop() => _tail;

    public IEnumerable<Term> GetConditions()
    {
        var conditions = new List<Term>();
        var current = this;

        // Collect conditions from top to bottom
        while (current != null)
        {
            conditions.Add(current.Condition);
            current = current.Pop();
        }

        // Return conditions in original order for better debugging
        return ((IEnumerable<Term>)conditions).Reverse();
    }

    public static ImmutableConditionStack Merge(IReadOnlyList<ImmutableConditionStack> stacks)
    {
        if (stacks.Count == 0)
        {
            throw new ArgumentException("At least one stack is required to merge.");
        }

        if (stacks.Count == 1)
        {
            return stacks[0];
        }

        // Find the largest stack to use as base
        var largestStackIndex = 0;
        var maxSize = stacks[0].Size;
        
        for (var i = 1; i < stacks.Count; i++)
        {
            if (stacks[i].Size > maxSize)
            {
                maxSize = stacks[i].Size;
                largestStackIndex = i;
            }
        }

        // Start with the largest stack
        var result = stacks[largestStackIndex];
        var baseStack = result;

        // Stack to help inserting additional conditions in original order (might be useful for debugging)
        var additionalConditions = new Stack<Term>();

        // Add conditions from other stacks
        for (var i = 0; i < stacks.Count; i++)
        {
            if (i == largestStackIndex)
            {
                continue;
            }

            var current = stacks[i];
            var commonBase = baseStack;

            // Make stacks the same height (commonBase is always the largest stack)
            while (commonBase != null && commonBase.Size > current.Size)
            {
                commonBase = commonBase.Pop();
            }

            // Find the point where stacks diverge
            while (current != null && commonBase != null && current != commonBase)
            {
                additionalConditions.Push(current.Condition);

                current = current.Pop();
                commonBase = commonBase.Pop();
            }

            // Push only the non-shared conditions
            while (additionalConditions.Count > 0)
            {
                result = result.Push(additionalConditions.Pop());
            }
        }

        return result;
    }
}
