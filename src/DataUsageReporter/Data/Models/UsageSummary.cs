namespace DataUsageReporter.Data.Models;

/// <summary>
/// Aggregated usage data for historical tracking. One record per hour.
/// </summary>
public class UsageSummary
{
    public long Id { get; set; }

    /// <summary>
    /// Unix epoch of hour start.
    /// </summary>
    public long PeriodStart { get; set; }

    /// <summary>
    /// Unix epoch of hour end.
    /// </summary>
    public long PeriodEnd { get; set; }

    /// <summary>
    /// Total bytes downloaded in period.
    /// </summary>
    public long TotalDownload { get; set; }

    /// <summary>
    /// Total bytes uploaded in period.
    /// </summary>
    public long TotalUpload { get; set; }

    /// <summary>
    /// Peak download speed (bytes/sec).
    /// </summary>
    public long PeakDownloadSpeed { get; set; }

    /// <summary>
    /// Peak upload speed (bytes/sec).
    /// </summary>
    public long PeakUploadSpeed { get; set; }

    /// <summary>
    /// Number of samples aggregated.
    /// </summary>
    public int SampleCount { get; set; }

    public UsageSummary() { }

    public UsageSummary(DateTime periodStart, DateTime periodEnd, long totalDownload, long totalUpload,
        long peakDownloadSpeed, long peakUploadSpeed, int sampleCount)
    {
        PeriodStart = new DateTimeOffset(periodStart.ToUniversalTime()).ToUnixTimeSeconds();
        PeriodEnd = new DateTimeOffset(periodEnd.ToUniversalTime()).ToUnixTimeSeconds();
        TotalDownload = totalDownload;
        TotalUpload = totalUpload;
        PeakDownloadSpeed = peakDownloadSpeed;
        PeakUploadSpeed = peakUploadSpeed;
        SampleCount = sampleCount;
    }

    public DateTime GetPeriodStart() => DateTimeOffset.FromUnixTimeSeconds(PeriodStart).LocalDateTime;
    public DateTime GetPeriodEnd() => DateTimeOffset.FromUnixTimeSeconds(PeriodEnd).LocalDateTime;
}
