namespace DataUsageReporter.Core;

/// <summary>
/// Formats byte counts and speeds for display using 1024-based units.
/// Speeds are displayed in bits per second.
/// </summary>
public class SpeedFormatter : ISpeedFormatter
{
    private static readonly string[] SizeUnits = { "B", "KB", "MB", "GB", "TB" };
    private static readonly string[] SpeedUnits = { "bps", "Kbps", "Mbps", "Gbps", "Tbps" };
    private const double UnitThreshold = 1024.0;

    public string FormatSpeed(long bytesPerSecond)
    {
        if (bytesPerSecond == 0)
            return "0 bps";

        // Convert bytes to bits (1 byte = 8 bits)
        long bitsPerSecond = bytesPerSecond * 8;
        return FormatValue(bitsPerSecond, SpeedUnits);
    }

    public string FormatBytes(long bytes)
    {
        if (bytes == 0)
            return "0 B";

        return FormatValue(bytes, SizeUnits);
    }

    private static string FormatValue(long value, string[] units)
    {
        double displayValue = Math.Abs(value);
        int unitIndex = 0;

        while (displayValue >= UnitThreshold && unitIndex < units.Length - 1)
        {
            displayValue /= UnitThreshold;
            unitIndex++;
        }

        // Use 1 decimal place for values >= 1, no decimals for smaller
        string format = displayValue >= 10 ? "0" : "0.#";
        string sign = value < 0 ? "-" : "";

        return $"{sign}{displayValue.ToString(format)} {units[unitIndex]}";
    }
}
