using System.Drawing;
using DataUsageReporter.Core;

namespace DataUsageReporter.UI;

/// <summary>
/// Manages system tray presence with NotifyIcon.
/// Displays real-time network speeds in tooltip.
/// </summary>
public class TrayIcon : ITrayIcon
{
    private readonly NotifyIcon _notifyIcon;
    private readonly SpeedDisplay _speedDisplay;
    private bool _disposed;

    public event EventHandler? OptionsRequested;
    public event EventHandler? ExitRequested;

    public TrayIcon(ISpeedFormatter formatter)
    {
        _speedDisplay = new SpeedDisplay(formatter);

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadApplicationIcon(),
            Visible = true,
            Text = "Data Usage Reporter\nStarting...",
            ContextMenuStrip = CreateContextMenu()
        };

        _notifyIcon.DoubleClick += (s, e) => OptionsRequested?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateSpeed(SpeedReading speed)
    {
        if (_disposed) return;

        var tooltip = _speedDisplay.FormatTooltip(speed);

        // NotifyIcon.Text has 128 character limit
        if (tooltip.Length > 127)
        {
            tooltip = tooltip[..127];
        }

        _notifyIcon.Text = tooltip;
    }

    public void ShowNotification(string title, string message, NotificationType type)
    {
        if (_disposed) return;

        var icon = type switch
        {
            NotificationType.Warning => ToolTipIcon.Warning,
            NotificationType.Error => ToolTipIcon.Error,
            _ => ToolTipIcon.Info
        };

        _notifyIcon.ShowBalloonTip(5000, title, message, icon);
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();

        var optionsItem = new ToolStripMenuItem("Options...");
        optionsItem.Click += (s, e) => OptionsRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(optionsItem);

        menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(exitItem);

        return menu;
    }

    private static Icon LoadApplicationIcon()
    {
        try
        {
            // Try to load the embedded application icon
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Resources", "app.ico");
            if (File.Exists(iconPath))
            {
                return new Icon(iconPath);
            }
        }
        catch
        {
            // Fall through to default icon
        }

        // Use system default application icon as fallback
        return SystemIcons.Application;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();

        GC.SuppressFinalize(this);
    }
}
