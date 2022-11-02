using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;
using Slicito.Presentation;

namespace Slicito.DotNet;

public partial class DotNetContext
{
    public IUriProvider IdeDetailUriProvider { get; } = new IdeUriProvider();

    private class IdeUriProvider : IUriProvider
    {
        public Uri? TryGetUriForElement(IElement element)
        {
            if (element is not DotNetSymbolElement symbolElement)
            {
                return null;
            }

            return symbolElement.Symbol.GetFileOpenUri();
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
