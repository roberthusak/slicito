using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;
using Slicito.Presentation;

namespace Slicito.DotNet;

public partial class DotNetContext
{
    public IUriProvider OpenInIdeUriProvider { get; } = new OpenInIdeUriProviderImplementation();

    private class OpenInIdeUriProviderImplementation : IUriProvider
    {
        public Uri? TryGetUriForElement(IElement element) =>
            element switch
            {
                DotNetSymbolElement { Symbol: var symbol } => symbol.GetFileOpenUri(),
                DotNetOperation { Operation: var operation } => operation.GetFileOpenUri(),
                _ => null
            };

        public Uri? TryGetUriForPair(object pair)
        {
            if (pair is not IPair<DotNetElement, DotNetElement, SyntaxNode?> sourcePair)
            {
                return null;
            }

            var syntaxNode = sourcePair.Data;
            if (syntaxNode is null)
            {
                return null;
            }

            var position = syntaxNode.SyntaxTree.GetMappedLineSpan(syntaxNode.Span);

            return ServerUtils.GetOpenFileEndpointUri(position);
        }
    }
}
