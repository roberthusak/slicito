using Microsoft.Msagl.Drawing;

using Slicito.Abstractions;

namespace Slicito.Presentation;

public class DefaultContextSiteBuilderOptions
{
    public Func<IElement, bool>? IndexElementDisplayFilter { get; set; }

    public Func<IElement, bool>? ElementRelationConnectionFilter { get; set; }

    public Func<IElement, bool>? ElementDetailPageCreationFilter { get; set; }

    public Action<IElement, Node>? ElementNodeCustomizer { get; set; }
}
