using VoidCore.Model.Text;

namespace LoadTest.Helpers;

public static class UriHelpers
{
    /// <summary>
    /// Will normalize local URLs to be fully-qualified, and will remove query strings and fragments.
    /// </summary>
    public static Uri? GetNormalizedUri(this string url, string primaryDomain, string[] primaryDomainEquivalents)
    {
        try
        {
            url = url.Trim();
            url = url.ToLowerInvariant();

            primaryDomain = primaryDomain.ToLowerInvariant();

            if (url.StartsWith("//"))
            {
                url = "https:" + url;
            }
            else if (url.StartsWith("http://"))
            {
                url = "https:" + url[5..];
            }

            if (url.StartsWith('/'))
            {
                url = "https://" + primaryDomain.TrimEnd('/') + url;
            }

            // TODO: How should we handle relatives like this? We would need more info like base and current URL. Currently handling from root.
            url = url.TrimStart('~');

            // Remove anything after the first # or ? (query or fragment)
            var index = url.IndexOfAny(['?', '#']);

            if (index > -1)
            {
                url = url[..index];
            }

            if (url.EndsWith('/') && url.Length > 1)
            {
                url = url.TrimEnd('/');
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            var uriBuilder = new UriBuilder(url);

            var alternateDomain = Array.Find(primaryDomainEquivalents, x => x.EqualsIgnoreCase(uriBuilder.Host));

            if (alternateDomain is not null)
            {
                uriBuilder.Host = primaryDomain;
            }

            return uriBuilder.Uri;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error normalizing {url}. {ex.Message}");
            return null;
        }
    }
}
