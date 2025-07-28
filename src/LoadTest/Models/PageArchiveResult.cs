namespace LoadTest.Models;

public class PageArchiveResult
{
    public long RequestCount { get; set; }

    public long MissedRequestCount { get; set; }

    public List<PageArchivePageResult> PageResults { get; set; } = [];
}
