using Microsoft.CodeAnalysis;

namespace Slicito.Roslyn;

public record InvocationInfo(IMethodSymbol Caller, IMethodSymbol Callee, SyntaxNode CallSite);
