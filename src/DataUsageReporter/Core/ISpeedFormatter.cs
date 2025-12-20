namespace DataUsageReporter.Core;

/// <summary>
/// Formats byte counts and speeds for display.
/// </summary>
public interface ISpeedFormatter
{
    /// <summary>
    /// Formats speed for taskbar display.
    /// </summary>
    /// <param name="bytesPerSecond">Speed in bytes per second</param>
    /// <returns>Formatted string like "1.5 MB/s"</returns>
    string FormatSpeed(long bytesPerSecond);

    /// <summary>
    /// Formats byte count for reports/graphs.
    /// </summary>
    /// <param name="bytes">Total bytes</param>
    /// <returns>Formatted string like "1.23 GB"</returns>
    string FormatBytes(long bytes);
}
