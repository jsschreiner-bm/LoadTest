using LoadTest.Helpers;
using PuppeteerSharp;

namespace LoadTest.Services;

public class HtmlContentRetriever : IDisposable
{
    private HttpClient? _httpClient;
    private IBrowser? _browser;
    private readonly PageArchiverConfiguration _config;
    private bool _disposedValue;

    public HtmlContentRetriever(PageArchiverConfiguration config)
    {
        _config = config;
    }

    public async Task Init()
    {
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        _browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            DefaultViewport = new ViewPortOptions
            {
                Width = 1920,
                Height = 1080
            }
        });
    }

    public async Task<HtmlContentRetrieverResult> GetContentAsync(Uri uri, CancellationToken cancellationToken)
    {
        // TODO: check if ContentType text/html in both cases
        return _config.UseBrowser ?
            await GetBrowserContentAsync(uri, cancellationToken) :
            await GetServerContentAsync(uri, cancellationToken);
    }

    private async Task<HtmlContentRetrieverResult> GetBrowserContentAsync(Uri uri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var page = await Browser.NewPageAsync();

        page.PageError += (sender, eventArgs) =>
        {
            if (_config.LogBrowserConsoleError)
            {
                Console.WriteLine($"Browser console error on {uri.OriginalString}: {eventArgs.Message}");
            }
        };

        var response = await page.GoToAsync(uri.OriginalString);

        response.EnsureSuccessStatusCode();

        if (_config.IsVerbose)
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

    private async Task<HtmlContentRetrieverResult> GetServerContentAsync(Uri uri, CancellationToken cancellationToken)
    {
        var client = HttpClient;

        var response = await client.GetAsync(uri, cancellationToken);

        response.EnsureSuccessStatusCode();

        if (_config.IsVerbose)
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

    private IBrowser Browser => _browser ?? throw new InvalidOperationException("Call Init() before calling GetContent()");

    private HttpClient HttpClient => _httpClient ??= new();

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
