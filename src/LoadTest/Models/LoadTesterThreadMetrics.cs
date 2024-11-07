namespace LoadTest.Models;

public struct LoadTesterThreadMetrics
{
    public long RequestCount { get; set; }

    public long MissedRequestCount { get; set; }
}
