namespace LoadTest.Helpers;

public static class UriHelpers
{

    /// <summary>
    /// Will normalize local URLs to be fully-qualified, and will remove query strings and fragments.
    /// </summary>
    public static Uri? GetNormalizedUri(this string url, string primaryUrlBase, string alternateDomains)
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

            if (url.Contains("example.com"))
            {
                // Removal all www then add www once to each
                url = url.Replace("www.example.com", "example.com", StringComparison.OrdinalIgnoreCase);
                url = url.Replace("example.com", "www.example.com", StringComparison.OrdinalIgnoreCase);
            }

            if (url.StartsWith('/'))
            {
                url = primaryUrlBase + url;
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

            return new Uri(url);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error normalizing {url}. {ex.Message}");
            return null;
        }
    }
}
