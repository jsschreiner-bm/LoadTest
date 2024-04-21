namespace LoadTest.Services;

public class PageArchiverPageResult
{
    public PageArchiverPageResult(Uri uri)
    {
        Url = uri;
    }

    public Uri Url { get; }
    public Uri? FinalUrl { get; set; }
    public bool IsRedirected { get; set; }
    public bool IsCrossDomainRedirect { get; set; }
    public bool IsError { get; set; }
    public bool IsOnlyFoundBySpider { get; set; }

    public bool IsKeywordInSelectedHtml { get; set; }
    public bool IsKeywordInSelectedHtmlInnerText { get; set; }

    public List<Uri> SpiderLinks { get; set; } = [];
}
