using System.Collections.Immutable;

namespace Slicito.Presentation;

public delegate Uri? GetUriDelegate(string locationId, IImmutableDictionary<string, string> parameters);

public partial class Site
{
    public const string IndexPageId = "index";

    public IEnumerable<string> StaticPageIds => _staticPages.Keys;

    public IEnumerable<string> DynamicPageIds => _dynamicPages.Keys;

    private readonly Dictionary<string, Func<PageNavigationOptions, IContent?>> _staticPages;
    private readonly Dictionary<string, Func<DynamicPageNavigationOptions, IContent?>> _dynamicPages;

    private Site(
        Dictionary<string, Func<PageNavigationOptions, IContent?>> staticPages,
        Dictionary<string, Func<DynamicPageNavigationOptions, IContent?>> dynamicPages)
    {
        _staticPages = staticPages;
        _dynamicPages = dynamicPages;
    }

    public IContent? NavigateTo(
        string pageId,
        IImmutableDictionary<string, string>? parameters = null,
        GetUriDelegate? getUriDelegate = null)
    {
        if (_staticPages.TryGetValue(pageId, out var staticPageCallback))
        {
            var options = new PageNavigationOptions(getUriDelegate);

            return staticPageCallback(options);
        }
        else if (_dynamicPages.TryGetValue(pageId, out var dynamicPageCallback))
        {
            var options = new DynamicPageNavigationOptions(
                getUriDelegate,
                parameters ?? ImmutableDictionary<string, string>.Empty);

            return dynamicPageCallback(options);
        }
        else
        {
            throw new ArgumentException($"Page with ID '{pageId}' was not found.", nameof(pageId));
        }
    }
}
