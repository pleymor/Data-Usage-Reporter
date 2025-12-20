namespace DataUsageReporter.Data.Models;

/// <summary>
/// Raw network usage sample collected every second.
/// Stored temporarily and aggregated into UsageSummary records.
/// </summary>
public class UsageRecord
{
    public long Id { get; set; }

    /// <summary>
    /// Unix epoch timestamp (seconds).
    /// </summary>
    public long Timestamp { get; set; }

    /// <summary>
    /// Cumulative bytes received at sample time.
    /// </summary>
    public long BytesReceived { get; set; }

    /// <summary>
    /// Cumulative bytes sent at sample time.
    /// </summary>
    public long BytesSent { get; set; }

    public UsageRecord() { }

    public UsageRecord(DateTime timestamp, long bytesReceived, long bytesSent)
    {
        Timestamp = new DateTimeOffset(timestamp.ToUniversalTime()).ToUnixTimeSeconds();
        BytesReceived = bytesReceived;
        BytesSent = bytesSent;
    }

    public DateTime GetDateTime() => DateTimeOffset.FromUnixTimeSeconds(Timestamp).LocalDateTime;
}
