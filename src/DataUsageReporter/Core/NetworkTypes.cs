namespace DataUsageReporter.Core;

/// <summary>
/// Aggregate network statistics across all physical adapters.
/// </summary>
public record NetworkStats(
    long TotalBytesReceived,
    long TotalBytesSent,
    DateTime Timestamp
);

/// <summary>
/// Current download and upload speeds in bytes per second.
/// </summary>
public record SpeedReading(
    long DownloadBytesPerSecond,
    long UploadBytesPerSecond,
    DateTime Timestamp
);
