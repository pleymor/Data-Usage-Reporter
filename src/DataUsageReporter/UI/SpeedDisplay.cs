using DataUsageReporter.Core;

namespace DataUsageReporter.UI;

/// <summary>
/// Helper for rendering stacked ↓/↑ speed format for tray icon tooltip.
/// </summary>
public class SpeedDisplay
{
    private readonly ISpeedFormatter _formatter;

    public SpeedDisplay(ISpeedFormatter formatter)
    {
        _formatter = formatter;
    }

    /// <summary>
    /// Formats speed reading as dual-line tooltip text.
    /// </summary>
    /// <returns>Format: "↓ X.X MB/s\n↑ X.X MB/s"</returns>
    public string FormatTooltip(SpeedReading speed)
    {
        var download = _formatter.FormatSpeed(speed.DownloadBytesPerSecond);
        var upload = _formatter.FormatSpeed(speed.UploadBytesPerSecond);
        return $"↓ {download}\n↑ {upload}";
    }

    /// <summary>
    /// Formats speed reading as single-line compact text.
    /// </summary>
    /// <returns>Format: "↓X.X ↑X.X MB/s"</returns>
    public string FormatCompact(SpeedReading speed)
    {
        var download = _formatter.FormatSpeed(speed.DownloadBytesPerSecond);
        var upload = _formatter.FormatSpeed(speed.UploadBytesPerSecond);
        return $"↓{download} ↑{upload}";
    }
}
