namespace LoadTest.Services;

public class PageArchiverResult
{
    public long RequestCount { get; set; }
    public long MissedRequestCount { get; set; }
    public List<PageArchiverPageResult> PageResults { get; set; } = [];
}

public static class PageArchiverResultExtensions
{
    public static PageArchiverResult Combine(this IEnumerable<PageArchiverResult> metricCollection) =>
        new PageArchiverResult().Combine(metricCollection);

    public static PageArchiverResult Combine(this PageArchiverResult metrics, IEnumerable<PageArchiverResult> metricCollection) =>
        metricCollection.Aggregate(metrics, (acc, x) => acc.Combine(x));

    public static PageArchiverResult Combine(this PageArchiverResult metrics, PageArchiverResult newMetrics) => new()
    {
        RequestCount = metrics.RequestCount + newMetrics.RequestCount,
        MissedRequestCount = metrics.MissedRequestCount + newMetrics.MissedRequestCount,
        PageResults = [.. metrics.PageResults, .. newMetrics.PageResults],
    };
}
