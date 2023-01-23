using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Formatting;

using Slicito.Abstractions;
using Slicito.Presentation;

namespace Slicito.Interactive;

public class SlicitoKernelExtension : IKernelExtension
{
    public Task OnLoadAsync(Kernel kernel)
    {
        Formatter.Register<Site>(InteractiveSession.Global.FormatSiteAsHtml, "text/html");
        Formatter.Register<IContext>(InteractiveSession.Global.FormatContextAsSiteHtml, "text/html");

        return Task.CompletedTask;
    }
}
