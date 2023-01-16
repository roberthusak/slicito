using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;
using Slicito.Interactive;
using Slicito.Presentation;

namespace Slicito.DotNet;

public partial class DotNetContext
{
    public IUriProvider GetOpenInIdeUriProvider(GetUriDelegate getUriDelegate) => new OpenInIdeUriProviderImplementation(getUriDelegate);

    private class OpenInIdeUriProviderImplementation : IUriProvider
    {
        private readonly GetUriDelegate _getUriDelegate;

        public OpenInIdeUriProviderImplementation(GetUriDelegate getUriDelegate)
        {
            _getUriDelegate = getUriDelegate;
        }

        public Uri? TryGetUriForElement(IElement element)
        {
            var location = element switch
            {
                DotNetSymbolElement { Symbol: var symbol } => symbol.TryGetDefinitionLocation(),
                DotNetOperation { Operation: var operation } => operation.Syntax.GetLocation().GetMappedLineSpan(),
                _ => null
            };

            if (location is null)
            {
                return null;
            }

            var destination = IdeUtils.GetOpenInIdePageNavigationDestination(location.Value);

            return _getUriDelegate(destination.PageId, destination.Parameters);
        }

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
