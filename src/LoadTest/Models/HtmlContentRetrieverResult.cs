namespace LoadTest.Models;

public class HtmlContentRetrieverResult
{
    public string FinalUrl { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string HtmlContent { get; set; } = string.Empty;
}
