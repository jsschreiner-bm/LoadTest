using VoidCore.Model.Text;

namespace LoadTest.Helpers;

public static class UriHelpers
{

    /// <summary>
    /// Will normalize local URLs to be fully-qualified, and will remove query strings and fragments.
    /// </summary>
    public static Uri? GetNormalizedUri(this string url, string primaryDomain, string[] alternateDomains)
    {
        try
        {
            url = url.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
            url = url.TrimStart('~');
            url = url.TrimEnd('/');
            url = url.ToLower();

            if (url.Length < 1)
            {
                url = "/";
            }

            if (url.StartsWith('/'))
            {
                url = "https://" + primaryDomain + url;
            }

            // Remove anything after the first # or ? (query or fragment)
            var index = url.IndexOfAny(['?', '#']);

            if (index > -1)
            {
                url = url[..index];
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            var uriBuilder = new UriBuilder(url);

            var alternateDomain = Array.Find(alternateDomains, x => x.EqualsIgnoreCase(uriBuilder.Host));

            if (alternateDomain is not null)
            {
                uriBuilder.Host = primaryDomain;
            }

            return new Uri(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error normalizing {url}. {ex.Message}");
            return null;
        }
    }
}
