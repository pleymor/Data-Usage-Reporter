using Microsoft.Win32;

namespace DataUsageReporter.Data;

/// <summary>
/// Manages Windows startup registry entry for automatic launch.
/// </summary>
public class StartupManager
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "DataUsageReporter";

    /// <summary>
    /// Gets whether the application is configured to start with Windows.
    /// </summary>
    public bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            var value = key?.GetValue(AppName);
            return value != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Enables or disables automatic startup with Windows.
    /// </summary>
    public void SetStartupEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath ?? Application.ExecutablePath;
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch
        {
            // Silently fail - registry access may be restricted
        }
    }
}
