using Cocona;
using Cocona.Application;
using LoadTest.Models;
using LoadTest.Services;

namespace LoadTest;

public class LoadTestCommands
{
    private readonly ICoconaAppContextAccessor _contextAccessor;

    public LoadTestCommands(ICoconaAppContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public CancellationToken CancellationToken => _contextAccessor?.Current?.CancellationToken ?? CancellationToken.None;

    [Command("save-urls", Description = "Save the sitemap as a list of URLs. Speeds up repeat runs.")]
    public async Task MakeList(
        [Option('p', Description = "URL to a sitemap or file path to a URL list. If file path ends in \".xml\", file is assumed a local copy of a sitemap.", ValueName = "path")]
        string path,
        [Option('o', Description = "File path to save output to.", ValueName = "output")]
        string output,
        [FromService] UrlsRetriever urlsRetriever)
    {
        await urlsRetriever.SaveUrlsAsync(path, output, CancellationToken);
    }

    [Command("load-test", Description = "Run a load test on a given set of URLs. Does not spider.")]
    public async Task Run(LoadTesterOptions options, [FromService] LoadTester loadTester)
    {
        await loadTester.RunLoadTestAsync(options, CancellationToken);
    }

    [Command("archive-pages", Description = "Save the HTML of pages.")]
    public async Task ArchivePages(PageArchiverOptions options, [FromService] PageArchiver pageArchiver)
    {
        await pageArchiver.ArchiveHtmlAsync(options, CancellationToken);
    }
}
