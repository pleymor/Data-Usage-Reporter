using DataUsageReporter.Core;

namespace DataUsageReporter.UI;

/// <summary>
/// Manages system tray presence and display.
/// </summary>
public interface ITrayIcon : IDisposable
{
    /// <summary>
    /// Updates the displayed speed values.
    /// </summary>
    void UpdateSpeed(SpeedReading speed);

    /// <summary>
    /// Shows a balloon notification.
    /// </summary>
    void ShowNotification(string title, string message, NotificationType type);

    /// <summary>
    /// Event raised when user requests options panel.
    /// </summary>
    event EventHandler? OptionsRequested;

    /// <summary>
    /// Event raised when user requests exit.
    /// </summary>
    event EventHandler? ExitRequested;
}

public enum NotificationType
{
    Info,
    Warning,
    Error
}
