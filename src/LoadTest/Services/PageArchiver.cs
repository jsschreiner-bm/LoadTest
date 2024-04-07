using CsvHelper;
using LoadTest.Helpers;
using System.Diagnostics;
using System.Globalization;
using VoidCore.Model.Text;

namespace LoadTest.Services;

public static class PageArchiver
{
    /// <summary>
    /// Saves a copy of the page HTML.
    /// </summary>
    public static async Task ArchiveHtmlAsync(PageArchiverConfiguration config, CancellationToken cancellationToken)
    {
        var csvFilePath = $"{config.OutputPath.TrimEnd('/')}/{DateTime.Now:yyyyMMdd_HHmmss}_{nameof(PageArchiver)}.csv";

        var uris = (await UrlsRetriever.GetUrlsAsync(config.SitemapUrl, cancellationToken))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.GetNormalizedUri(config.PrimaryUrlBase))
            .Where(x => x is not null)
            .Where(x => !config.ExcludedPaths.Exists(y => x!.PathAndQuery.StartsWith(y)))
            .Select(x => x!)
            .ToArray();

        if (uris.Length == 0)
        {
            Console.WriteLine("No URLs found. Exiting.");
            return;
        }

        Console.WriteLine("Performing HTML archive. Press Ctrl+C to stop.");

        using var client = new HtmlContentRetriever(config);
        await client.Init();

        var startTime = Stopwatch.GetTimestamp();

        var jobResult = new PageArchiverResult();
        var spiderLinksCount = 0;
        var passes = 0;

        do
        {
            passes++;

            var isSpiderPass = passes > 1;

            var tasks = Enumerable
                .Range(0, config.ThreadCount)
                .Select(i => StartThreadAsync(i, uris, config, client, isSpiderPass, cancellationToken))
                .ToArray();

            var passResult = (await Task.WhenAll(tasks)).Combine();

            var previousUrls = jobResult.PageResults
                .Select(y => y.Url.ToString())
                .ToArray();

            var newSpiderLinks = passResult.PageResults
                .SelectMany(x => x.SpiderLinks)
                .Distinct()
                .Where(x => !config.ExcludedPaths.Exists(y => x.PathAndQuery.StartsWith(y)) && !previousUrls.Contains(x.ToString(), StringComparer.OrdinalIgnoreCase))
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

        var missedPercent = (double)jobResult.MissedRequestCount / jobResult.RequestCount * 100;
        Console.WriteLine($"{jobResult.MissedRequestCount} unintended missed requests = {missedPercent:F2}%");

        using var writer = new StreamWriter(csvFilePath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(jobResult.PageResults);
    }

    private static async Task<PageArchiverResult> StartThreadAsync(int threadNumber, Uri[] urls, PageArchiverConfiguration config, HtmlContentRetriever client, bool isSpiderPass, CancellationToken cancellationToken)
    {
        (var initialUrlIndex, var stopUrlIndex) = ThreadHelpers.GetBlockStartAndEnd(threadNumber, config.ThreadCount, urls.Length);

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
                IsFoundBySpider = isSpiderPass
            };

            threadResult.PageResults.Add(pageResult);

            threadResult.RequestCount++;

            try
            {
                // TODO: return final URL, html content and status code?
                var page = await client.GetContentAsync(uri, cancellationToken);

                // TODO: normalize
                pageResult.FinalUrl = page.FinalUrl;

                await SaveHtmlContent(config, uri, page.HtmlContent, cancellationToken);

                // TODO: search

                // TODO: spider, normalize URLs.
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error archiving {uri.OriginalString}. {ex.Message}");
                threadResult.MissedRequestCount++;
            }

            if (urlIndex == stopUrlIndex)
            {
                // Stop because we hit all the URLs once.
                break;
            }

            // Get the next URL, looping around to beginning if at the end.
            urlIndex++;

            if (config.IsDelayEnabled)
            {
                await Task.Delay(500, cancellationToken);
            }
        }

        if (config.IsVerbose)
        {
            Console.WriteLine($"Thread {threadNumber} ending.");
        }

        return threadResult;
    }

    private static async Task SaveHtmlContent(PageArchiverConfiguration config, Uri uri, string content, CancellationToken cancellationToken)
    {
        if (!config.SaveHtml)
        {
            return;
        }

        var htmlFileFolder = GetHtmlFileFolder(config, uri);
        var htmlFileName = GetHtmlFileName(uri);
        var htmlFilePath = Path.Combine(htmlFileFolder, htmlFileName);

        if (config.IsVerbose)
        {
            Console.WriteLine($"Writing {content.Length} chars to {htmlFilePath}");
        }

        Directory.CreateDirectory(htmlFileFolder);
        await File.WriteAllTextAsync(htmlFilePath, content, cancellationToken);
    }

    private static string GetHtmlFileFolder(PageArchiverConfiguration config, Uri uri)
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
