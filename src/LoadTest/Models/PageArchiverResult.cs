namespace LoadTest.Models;

public class PageArchiverResult
{
    public long RequestCount { get; set; }

    public long MissedRequestCount { get; set; }

    public List<PageArchiverPageResult> PageResults { get; set; } = [];
}
