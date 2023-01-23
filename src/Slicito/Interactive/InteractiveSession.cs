using System.Collections.Immutable;
using System.Globalization;
using System.Web;

using Microsoft.DotNet.Interactive;

using Slicito.Abstractions;
using Slicito.Presentation;

namespace Slicito.Interactive;

public class InteractiveSession
{
    public static InteractiveSession Global = new();

    private readonly Dictionary<Guid, Site> _sites = new();
    private readonly Dictionary<Guid, PageNavigationDestination> _destinations = new();

    public Guid AddSite(Site site)
    {
        var guid = Guid.NewGuid();
        _sites.Add(guid, site);

        return guid;
    }

    public Site GetSite(Guid guid) => _sites[guid];

    public Guid AddDestination(PageNavigationDestination destination)
    {
        var guid = Guid.NewGuid();
        _destinations.Add(guid, destination);

        return guid;
    }

    public PageNavigationDestination GetDestination(Guid guid) => _destinations[guid];

    public void Reset() => _sites.Clear();

    public Uri StoreDestinationAndGetUri(string pageId, IImmutableDictionary<string, string> parameters)
    {
        var guid = AddDestination(new(pageId, parameters));

        return new($"#{guid}", UriKind.Relative);
    }

    public void FormatSiteAsHtml(Site site, TextWriter writer)
    {
        var frontPageId = site.StaticPageIds.Min();
        if (frontPageId == null)
        {
            return;
        }

        var frontPageContent = site.NavigateTo(frontPageId, null, InteractiveSession.Global.StoreDestinationAndGetUri);

        if (frontPageContent is null)
        {
            return;
        }

        var guid = AddSite(site);

        writer.WriteLine($"<div class='slicito-site' id='slicito-site-{guid}'>");
        writer.WriteLine($"<button class='slicito-site-back' style='display:none' onclick='window.slicito.showPreviousSiteContent(\"{guid}\")'>Back</button>");
        writer.WriteLine($"<div class='slicito-site-content' id='slicito-site-content-{guid}'>");

        frontPageContent.WriteHtmlTo(writer);

        writer.WriteLine("</div>");
        writer.WriteLine("</div>");

        writer.WriteLine("<script type=\"text/javascript\">");
        writer.WriteLine(Resources.SiteNavigationJavaScript);
        writer.WriteLine($"window.slicito.forwardLinksToDotNet(\"{guid}\");");
        writer.WriteLine("</script>");
    }

    public void FormatContextAsSiteHtml(IContext context, TextWriter writer) =>
        FormatSiteAsHtml(new DefaultContextSiteBuilder(context).Build(), writer);

    public async Task NavigateSiteToAsync(Guid siteGuid, Guid destinationGuid)
    {
        var site = GetSite(siteGuid);
        var destination = GetDestination(destinationGuid);

        var content = site.NavigateTo(destination.PageId, destination.Parameters, StoreDestinationAndGetUri);
        var jsKernel = Kernel.Root.FindKernelByName("javascript");

        if (content is null || jsKernel is null)
        {
            return;
        }

        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        content.WriteHtmlTo(writer);
        
        var contentHtml = writer.ToString();
        var contentJsString = HttpUtility.JavaScriptStringEncode(contentHtml, addDoubleQuotes: false);

        await jsKernel.SubmitCodeAsync($@"window.slicito.showSiteContent(""{siteGuid}"", ""{contentJsString}"");");
    }

    public void OpenFileInIde(string path, int line, int offset)
    {
        // Inspired by https://stackoverflow.com/a/54869165/2105235
        // (other ways are cleaner but need proper handling of NuGet packages etc.)

        dynamic vs = Marshal2.GetActiveObject("VisualStudio.DTE");
        dynamic window = vs.ItemOperations.OpenFile(path);
        window.Selection.MoveToLineAndOffset(line, offset);
    }
}
