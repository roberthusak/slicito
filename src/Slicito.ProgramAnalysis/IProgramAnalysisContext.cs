using Slicito.Abstractions;

namespace Slicito.ProgramAnalysis;

public interface IProgramAnalysisContext : ISlicitoContext
{
    IProgramTypes ProgramTypes { get; }

    ICallGraphProvider CallGraphProvider { get; }
}
