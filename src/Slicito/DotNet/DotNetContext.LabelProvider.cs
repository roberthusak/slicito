using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.DotNet.Elements;
using Slicito.Presentation;

namespace Slicito.DotNet;

public partial class DotNetContext
{
    public ILabelProvider LabelProvider { get; } = new DotNetLabelProvider();

    private class DotNetLabelProvider : ILabelProvider
    {
        public string? TryGetLabelForElement(IElement element, IElement? containingElement) =>
            element switch
            {
                DotNetProject projectElement =>
                    Path.GetFileName(projectElement.Project.FilePath),
                DotNetSymbolElement { Symbol: var symbol } =>
                    symbol.GetShortName(TryGetContainingElementSymbol(containingElement)),
                DotNetOperation { Operation: var operation } =>
                    operation.Kind.ToString(),
                _ =>
                    null
            };

        public string? TryGetLabelForPair(object pair) => null;

        private static ISymbol? TryGetContainingElementSymbol(IElement? element) =>
            element switch
            {
                DotNetProject projectElement => projectElement.Compilation.GlobalNamespace,
                DotNetSymbolElement { Symbol: var symbol } => symbol,
                _ => null
            };
    }
}
