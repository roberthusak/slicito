namespace Slicito.Presentation;

public partial class Site
{
    public class Builder
    {
        private readonly Dictionary<string, Func<PageNavigationOptions, IContent?>> _staticPageCallbacks = new();
        private readonly Dictionary<string, Func<DynamicPageNavigationOptions, IContent?>> _dynamicPageCallbacks = new();

        public Builder AddStaticPage(string pageId, Func<PageNavigationOptions, IContent?> callback)
        {
            EnsurePageIdUniqueness(pageId);

            _staticPageCallbacks.Add(pageId, callback);

            return this;
        }

        public Builder AddDynamicPage(string pageId, Func<DynamicPageNavigationOptions, IContent?> callback)
        {
            EnsurePageIdUniqueness(pageId);

            _dynamicPageCallbacks.Add(pageId, callback);

            return this;
        }

        public Site Build() => new(
            new Dictionary<string, Func<PageNavigationOptions, IContent?>>(_staticPageCallbacks),
            new Dictionary<string, Func<DynamicPageNavigationOptions, IContent?>>(_dynamicPageCallbacks));

        private void EnsurePageIdUniqueness(string pageId)
        {
            if (_staticPageCallbacks.ContainsKey(pageId) || _dynamicPageCallbacks.ContainsKey(pageId))
            {
                throw new ArgumentException($"The page with the ID '{pageId}' already exists.", nameof(pageId));
            }
        }
    }
}
