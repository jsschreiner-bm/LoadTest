using Cocona;

namespace LoadTest.Models;

public class LoadTesterOptions : ICommandParameterSet
{
    [Option('p', Description = "URL to a sitemap or file path to a URL list. If file path ends in \".xml\", file is assumed a local copy of a sitemap.", ValueName = "path")]
    public string SitemapUrl { get; set; } = string.Empty;

    [Option('t', Description = "Number of concurrent threads to make requests.", ValueName = "threads")]
    [HasDefaultValue]
    public int ThreadCount { get; init; } = 2;

    [Option('s', Description = "Number of seconds to run before stopping. If zero, requests all URLs once.", ValueName = "seconds")]
    [HasDefaultValue]
    public int SecondsToRun { get; init; } = 5;

    [Option('e', Description = "Percent chance of an intentional page miss.", ValueName = "chance-404")]
    [HasDefaultValue]
    public int ChanceOf404 { get; init; }

    [Option('d', Description = "Add delay between requests.", ValueName = "delay")]
    public bool IsDelayEnabled { get; init; }

    [Option('m', Description = "Change the request method.", ValueName = "method")]
    [HasDefaultValue]
    public HttpMethod RequestMethod { get; init; } = HttpMethod.Get;

    [Option(Description = "Show more logging.", ValueName = "verbose")]
    public bool IsVerbose { get; init; }
}
