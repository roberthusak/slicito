using Microsoft.CodeAnalysis;

using Slicito.Abstractions;
using Slicito.Abstractions.Relations;
using Slicito.DotNet.Elements;
using Slicito.Presentation;

namespace Slicito.DotNet;

public partial class DotNetContext
{
    public ILabelProvider LabelProvider { get; } = new DotNetLabelProvider();

    private class DotNetLabelProvider : ILabelProvider
    {
        public string? TryGetLabelForElement(IElement element) =>
            element switch
            {
                DotNetProject projectElement => Path.GetFileName(projectElement.Project.FilePath),
                DotNetSymbolElement symbolElement => symbolElement.Symbol.GetNodeLabelText(),
                _ => null
            };

        public string? TryGetLabelForPair(object pair) => null;
    }
}
