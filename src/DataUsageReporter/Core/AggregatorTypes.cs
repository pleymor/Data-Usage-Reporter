namespace DataUsageReporter.Core;

/// <summary>
/// Time granularity for displaying usage data.
/// </summary>
public enum TimeGranularity
{
    Minute,
    Hour,
    Day,
    Week,
    Month,
    Year
}

/// <summary>
/// A single data point for graph display.
/// </summary>
public record UsageDataPoint(
    DateTime Timestamp,
    long DownloadBytes,
    long UploadBytes
);
