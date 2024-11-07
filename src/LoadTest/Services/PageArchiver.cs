using CsvHelper;
using LoadTest.Helpers;
using LoadTest.Models;
using System.Diagnostics;
using System.Globalization;
using VoidCore.Model.Text;

namespace LoadTest.Services;

public class PageArchiver
{
    private readonly UrlsRetriever _urlsRetriever;
    private readonly HtmlContentRetriever _htmlContentRetriever;

    public PageArchiver(UrlsRetriever urlsRetriever, HtmlContentRetriever htmlContentRetriever)
    {
        _urlsRetriever = urlsRetriever;
        _htmlContentRetriever = htmlContentRetriever;
    }

    /// <summary>
    /// Saves a copy of the page HTML.
    /// </summary>
    public async Task ArchiveHtmlAsync(PageArchiverOptions options, CancellationToken cancellationToken)
    {
        var csvFilePath = $"{options.OutputPath.TrimEnd('/')}/{DateTime.Now:yyyyMMdd_HHmmss}_{nameof(PageArchiver)}.csv";

        var uris = (await _urlsRetriever.GetUrlsAsync(options.SitemapUrl, cancellationToken))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.GetNormalizedUri(options.PrimaryDomain, options.PrimaryDomainEquivalents))
            .Where(x => x is not null && PathIsNotExcluded(options, x))
            .Select(x => x!)
            .ToArray();

        if (uris.Length == 0)
        {
            Console.WriteLine("No URLs found. Exiting.");
            return;
        }

        Console.WriteLine("Performing HTML archive. Press Ctrl+C to stop.");

        var startTime = Stopwatch.GetTimestamp();

        var jobResult = new PageArchiverResult();
        var spiderLinksCount = 0;
        var passes = 0;

        do
        {
            passes++;

            var isSpiderPass = passes > 1;

            var tasks = Enumerable
                .Range(0, options.ThreadCount)
                .Select(i => StartThreadAsync(i, uris, options, _htmlContentRetriever, isSpiderPass, cancellationToken))
                .ToArray();

            var passResult = (await Task.WhenAll(tasks)).Combine();

            var previousUrls = jobResult.PageResults
                .Select(y => y.Url.ToString())
                .ToArray();

            var newSpiderLinks = passResult.PageResults
                .SelectMany(x => x.SpiderLinks)
                .Distinct()
                .Where(x => x is not null && PathIsNotExcluded(options, x) && !Array.Exists(previousUrls, y => y.EqualsIgnoreCase(x.ToString())))
                .ToArray();

            uris = newSpiderLinks;
            spiderLinksCount += newSpiderLinks.Length;

            jobResult = jobResult.Combine(passResult);
        } while (uris.Length > 0);

        if (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine("Cancelled.");
        }
        else
        {
            Console.WriteLine("Finished.");
        }

        var elapsedTime = Stopwatch.GetElapsedTime(startTime);
        var seconds = elapsedTime.TotalMilliseconds / 1000;
        var safeSeconds = seconds == 0 ? 1 : seconds;

        Console.WriteLine($"{jobResult.RequestCount} requests in {elapsedTime} = {jobResult.RequestCount / safeSeconds:F2} RPS");

        if (options.IsSpiderEnabled)
        {
            Console.WriteLine($"Spider found {spiderLinksCount} pages that weren't in the original list. Took {passes} passes.");
        }

        var missedPercent = (double)jobResult.MissedRequestCount / jobResult.RequestCount * 100;
        Console.WriteLine($"{jobResult.MissedRequestCount} unintended missed requests = {missedPercent:F2}%");

        await using var writer = new StreamWriter(csvFilePath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(jobResult.PageResults);
    }

    private static bool PathIsNotExcluded(PageArchiverOptions config, Uri x)
    {
        return !config.ExcludedPaths.Exists(x.PathAndQuery.StartsWith);
    }

    private static async Task<PageArchiverResult> StartThreadAsync(int threadNumber, Uri[] urls, PageArchiverOptions options, HtmlContentRetriever client, bool isSpiderPass, CancellationToken cancellationToken)
    {
        (var initialUrlIndex, var stopUrlIndex) = ThreadHelpers.GetBlockStartAndEnd(threadNumber, options.ThreadCount, urls.Length);

        var threadResult = new PageArchiverResult();

        if (initialUrlIndex == -1)
        {
            return threadResult;
        }

        var urlIndex = initialUrlIndex;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var uri = urls[urlIndex] ?? throw new InvalidOperationException($"URL at index {urlIndex} is null.");

            var pageResult = new PageArchiverPageResult(uri)
            {
                IsOnlyFoundBySpider = isSpiderPass
            };

            threadResult.PageResults.Add(pageResult);

            threadResult.RequestCount++;

            try
            {
                var page = await client.GetContentAsync(options, uri, cancellationToken);

                pageResult.FinalUrl = page.FinalUrl.GetNormalizedUri(options.PrimaryDomain, options.PrimaryDomainEquivalents);

                if (pageResult.FinalUrl is not null)
                {
                    // Uri equivalency uses .ToString()
                    // We can assume they are both normalized, so we don't need to ignore casing.
                    pageResult.IsRedirected = uri != pageResult.FinalUrl;
                    pageResult.IsCrossDomainRedirect = pageResult.FinalUrl.Host != uri.Host;
                }

                if (options.ScanCrossDomainRedirects || !pageResult.IsCrossDomainRedirect)
                {
                    await SaveHtmlContent(options, uri, page.HtmlContent, cancellationToken);

                    // TODO: search, include/exclude selectors

                    // TODO: spider, normalize URLs.
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error archiving {uri.OriginalString}. {ex.Message}");
                pageResult.IsError = true;
                threadResult.MissedRequestCount++;
            }

            if (urlIndex == stopUrlIndex)
            {
                // Stop because we hit all the URLs once.
                break;
            }

            // Get the next URL, looping around to beginning if at the end.
            urlIndex++;

            if (options.IsDelayEnabled)
            {
                await Task.Delay(500, cancellationToken);
            }
        }

        if (options.IsVerbose)
        {
            Console.WriteLine($"Thread {threadNumber} ending.");
        }

        return threadResult;
    }

    private static async Task SaveHtmlContent(PageArchiverOptions options, Uri uri, string content, CancellationToken cancellationToken)
    {
        var htmlFileFolder = GetHtmlFileFolder(options, uri);
        var htmlFileName = GetHtmlFileName(uri);
        var htmlFilePath = Path.Combine(htmlFileFolder, htmlFileName);

        if (options.IsVerbose)
        {
            Console.WriteLine($"Writing {content.Length} chars to {htmlFilePath}");
        }

        Directory.CreateDirectory(htmlFileFolder);
        await File.WriteAllTextAsync(htmlFilePath, content, cancellationToken);
    }

    private static string GetHtmlFileFolder(PageArchiverOptions config, Uri uri)
    {
        return Path.Combine(
            config.OutputPath,
            "html",
            string.Concat(uri.Segments[..^1]).TrimStart('/'))
        .GetSafeFilePath();
    }

    private static string GetHtmlFileName(Uri uri)
    {
        var lastUriSegment = uri.Segments[^1];

        if (lastUriSegment.IsNullOrWhiteSpace() || lastUriSegment == "/")
        {
            lastUriSegment = "index";
        }

        lastUriSegment = lastUriSegment.TrimEnd('/');

        return (lastUriSegment + ".html").GetSafeFileName();
    }
}
