using LoadTest.Helpers;
using LoadTest.Models;
using PuppeteerSharp;

namespace LoadTest.Services;

public class HtmlContentRetriever : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore = new(1);
    private IBrowser? _browser;
    private bool _disposedValue;

    public HtmlContentRetriever(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HtmlContentRetrieverResult> GetContentAsync(PageArchiveOptions config, Uri uri, CancellationToken cancellationToken)
    {
        // TODO: check if ContentType text/html in both cases
        return config.UseBrowser ?
            await GetBrowserContentAsync(config, uri, cancellationToken) :
            await GetServerContentAsync(config, uri, cancellationToken);
    }

    private async Task<HtmlContentRetrieverResult> GetBrowserContentAsync(PageArchiveOptions config, Uri uri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var browser = await EnsureBrowserAsync(cancellationToken);

        await using var page = await browser.NewPageAsync();

        page.PageError += (sender, eventArgs) =>
        {
            if (config.LogBrowserConsoleErrors)
            {
                Console.WriteLine($"Browser console error on {uri.OriginalString}: {eventArgs.Message}");
            }
        };

        var response = await page.GoToAsync(uri.OriginalString);

        response.EnsureSuccessStatusCode();

        if (config.IsVerbose)
        {
            Console.WriteLine($"{response.Status} {uri.OriginalString}");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Wait for JS to render (you may need to adjust the wait time)
        // Alternatively, you could listen for JavaScript event if you can make the app emit one when it's done with initial rendering.
        await page.WaitForTimeoutAsync(100);

        // TODO: test redirects and failed requests
        return new HtmlContentRetrieverResult
        {
            FinalUrl = response.Url,
            StatusCode = (int)response.Status,
            HtmlContent = await page.GetContentAsync(),
        };
    }

    private async Task<HtmlContentRetrieverResult> GetServerContentAsync(PageArchiveOptions config, Uri uri, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(uri, cancellationToken);

        response.EnsureSuccessStatusCode();

        if (config.IsVerbose)
        {
            Console.WriteLine($"{response.StatusCode} {uri.OriginalString}");
        }

        // TODO: test redirects and failed requests
        return new HtmlContentRetrieverResult
        {
            FinalUrl = response.RequestMessage?.RequestUri?.OriginalString ?? string.Empty,
            StatusCode = (int)response.StatusCode,
            HtmlContent = await response.Content.ReadAsStringAsync(cancellationToken),
        };
    }

    private async Task<IBrowser> EnsureBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser is not null)
        {
            return _browser;
        }

        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return _browser ??= await CreateBrowserAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private static async Task<IBrowser> CreateBrowserAsync()
    {
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        return await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            DefaultViewport = new ViewPortOptions
            {
                Width = 1920,
                Height = 1080
            }
        });
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _browser?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
