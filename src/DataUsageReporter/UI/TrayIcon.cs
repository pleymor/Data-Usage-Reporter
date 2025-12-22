using System.Drawing;
using DataUsageReporter.Core;
using DataUsageReporter.Core.Localization;

namespace DataUsageReporter.UI;

/// <summary>
/// Manages system tray presence with NotifyIcon.
/// Displays real-time network speeds in tooltip.
/// </summary>
public class TrayIcon : ITrayIcon
{
    private readonly NotifyIcon _notifyIcon;
    private readonly SpeedDisplay _speedDisplay;
    private readonly ILocalizationService _localization;
    private readonly Icon? _appIcon;
    private ToolStripMenuItem? _optionsItem;
    private ToolStripMenuItem? _exitItem;
    private bool _disposed;

    public event EventHandler? OptionsRequested;
    public event EventHandler? ExitRequested;

    public TrayIcon(ISpeedFormatter formatter, ILocalizationService localization)
    {
        _speedDisplay = new SpeedDisplay(formatter);
        _localization = localization;
        _appIcon = LoadApplicationIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Visible = true,
            Text = "Data Usage Reporter\nStarting...",
            ContextMenuStrip = CreateContextMenu()
        };

        _notifyIcon.DoubleClick += (s, e) => OptionsRequested?.Invoke(this, EventArgs.Empty);

        // Subscribe to language changes
        _localization.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        RefreshStrings();
    }

    private void RefreshStrings()
    {
        if (_optionsItem != null)
            _optionsItem.Text = _localization.GetString("Menu_Options") + "...";
        if (_exitItem != null)
            _exitItem.Text = _localization.GetString("Menu_Exit");
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

        _optionsItem = new ToolStripMenuItem(_localization.GetString("Menu_Options") + "...");
        _optionsItem.Click += (s, e) => OptionsRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(_optionsItem);

        menu.Items.Add(new ToolStripSeparator());

        _exitItem = new ToolStripMenuItem(_localization.GetString("Menu_Exit"));
        _exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(_exitItem);

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

        // Unsubscribe from events
        _localization.LanguageChanged -= OnLanguageChanged;

        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();

        // Dispose icon if it's not a system icon
        if (_appIcon != null && _appIcon != SystemIcons.Application)
        {
            _appIcon.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}
