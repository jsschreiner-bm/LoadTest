namespace LoadTest.Services;

public class PageArchiverConfiguration
{
    public string SitemapUrl { get; set; } = string.Empty;
    public string OutputPath { get; init; } = string.Empty;
    public bool SaveHtml { get; set; }
    public bool UseBrowser { get; set; }
    public bool LogBrowserConsoleError { get; set; }
    public int ThreadCount { get; init; }
    public bool IsDelayEnabled { get; init; }
    public bool IsVerbose { get; init; }
    // TODO: check for unused props
    public bool IsSpiderEnabled { get; init; }
    public string PrimaryDomain { get; set; } = string.Empty;
    public List<string> PrimaryDomainEquivalents { get; init; } = [];
    public bool ScanCrossDomainRedirects { get; set; }
    public List<string> ExcludedPaths { get; init; } = [];
    // TODO: for search
    public List<string> SearchTerms { get; init; } = [];
    public string MainContentSelector { get; init; } = string.Empty;
    public string ExcludeContentSelector { get; init; } = string.Empty;
}
