using Cocona;

namespace LoadTest.Models;

public class PageArchiveOptions : ICommandParameterSet
{
    [Option("path", ['p'], Description = "URL to a sitemap or file path to a URL list. If file path ends in \".xml\", file is assumed a local copy of a sitemap.", ValueName = "path")]
    public string SitemapUrl { get; init; } = string.Empty;

    [Option("output", ['o'], Description = "File path to save output to.", ValueName = "output")]
    public string OutputPath { get; init; } = string.Empty;

    [Option("browser", ['b'], Description = "Use browser (Puppeteer) to fully render pages, including JS. Otherwise only HTML source is considered.", ValueName = "browser")]
    public bool UseBrowser { get; init; }

    [Option("browser-errors", ['e'], Description = "Log browser console errors.", ValueName = "browser-errors")]
    public bool LogBrowserConsoleErrors { get; init; }

    [Option("threads", ['t'], Description = "Number of concurrent threads to make requests.", ValueName = "threads")]
    [HasDefaultValue]
    public int ThreadCount { get; init; } = 2;

    [Option("delay", ['d'], Description = "Add delay between requests.", ValueName = "delay")]
    public bool IsDelayEnabled { get; init; }

    [Option("verbose", ['v'], Description = "Show more logging.", ValueName = "verbose")]
    public bool IsVerbose { get; init; }

    [Option("spider", Description = "Enable spider (find local pages linked from other pages).", ValueName = "spider")]
    public bool IsSpiderEnabled { get; init; }

    [Option("domain", Description = "Primary domain to archive.", ValueName = "domain")]
    [HasDefaultValue]
    public string? PrimaryDomain { get; init; }

    [Option("domain-alts", Description = "Primary domain equivalents.", ValueName = "domain-equiv")]
    [HasDefaultValue]
    public string[]? PrimaryDomainEquivalents { get; init; }

    [Option("cross-domain", Description = "Scan cross domain redirects.", ValueName = "cross-domain")]
    public bool ScanCrossDomainRedirects { get; init; }

    [Option("exclude-paths", Description = "Exclude URL paths.", ValueName = "exclude")]
    [HasDefaultValue]
    public List<string>? ExcludedPaths { get; init; }

    [Option("content-include", Description = "Only save the HTML element matching this CSS selector. For example, body or main. Whole document is saved by default.", ValueName = "main-content")]
    [HasDefaultValue]
    public string? MainContentSelector { get; init; }

    [Option("content-exclude", Description = "Exclude elements from the main content that match this CSS selector.", ValueName = "exclude-content")]
    [HasDefaultValue]
    public string? SearchExcludeContentSelector { get; init; }

    [Option("content-search", Description = "Only save selected content if it contains one of these search terms.", ValueName = "search")]
    [HasDefaultValue]
    public List<string>? SearchTerms { get; init; }
}
