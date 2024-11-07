using Cocona;

namespace LoadTest.Models;

public class PageArchiverOptions : ICommandParameterSet
{
    [Option('p', Description = "URL to a sitemap or file path to a URL list. If file path ends in \".xml\", file is assumed a local copy of a sitemap.", ValueName = "path")]
    public string SitemapUrl { get; init; } = string.Empty;

    [Option('o', Description = "File path to save output to.", ValueName = "output")]
    public string OutputPath { get; init; } = string.Empty;

    [Option('b', Description = "Use browser (Puppeteer) to fully render pages, including JS.", ValueName = "browser")]
    public bool UseBrowser { get; init; }

    [Option('e', Description = "Log browser console errors.", ValueName = "browser-errors")]
    public bool LogBrowserConsoleError { get; init; }

    [Option('t', Description = "Number of concurrent threads to make requests.", ValueName = "threads")]
    [HasDefaultValue]
    public int ThreadCount { get; init; } = 2;

    [Option('d', Description = "Add delay between requests.", ValueName = "delay")]
    public bool IsDelayEnabled { get; init; }

    [Option(Description = "Show more logging.", ValueName = "verbose")]
    public bool IsVerbose { get; init; }

    [Option(Description = "Enable spider (find local pages linked from other pages).", ValueName = "spider")]
    public bool IsSpiderEnabled { get; init; }

    [Option(Description = "Primary domain to archive.", ValueName = "domain")]
    [HasDefaultValue]
    public string? PrimaryDomain { get; init; }

    [Option(Description = "Domain equivalents.", ValueName = "domain-equiv")]
    [HasDefaultValue]
    public string[]? PrimaryDomainEquivalents { get; init; }

    [Option(Description = "Scan cross domain redirects.", ValueName = "cross-domain")]
    public bool ScanCrossDomainRedirects { get; init; }

    [Option(Description = "Exclude URL paths.", ValueName = "exclude")]
    [HasDefaultValue]
    public List<string>? ExcludedPaths { get; init; }

    [Option(Description = "Only save pages with these search terms.", ValueName = "search")]
    [HasDefaultValue]
    public List<string>? SearchTerms { get; init; }

    [Option(Description = "Only save the HTML element matching this CSS selector.", ValueName = "main-content")]
    [HasDefaultValue]
    public string? SearchMainContentSelector { get; init; }

    [Option(Description = "Exclude elements matching this CSS selector.", ValueName = "exclude-content")]
    [HasDefaultValue]
    public string? SearchExcludeContentSelector { get; init; }
}
