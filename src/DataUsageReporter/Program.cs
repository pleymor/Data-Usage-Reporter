using DataUsageReporter.Core;
using DataUsageReporter.Data;
using DataUsageReporter.Data.Models;
using DataUsageReporter.Email;
using DataUsageReporter.UI;
using Microsoft.Win32;

namespace DataUsageReporter;

static class Program
{
    private const string MutexName = "DataUsageReporter_SingleInstance";
    private static Mutex? _mutex;
    private static System.Windows.Forms.Timer? _monitorTimer;
    private static System.Windows.Forms.Timer? _aggregationTimer;
    private static INetworkMonitor? _networkMonitor;
    private static ITrayIcon? _trayIcon;
    private static IUsageRepository? _usageRepository;
    private static ISpeedFormatter? _speedFormatter;
    private static IUsageAggregator? _usageAggregator;
    private static ISettingsRepository? _settingsRepository;
    private static ICredentialManager? _credentialManager;
    private static RetentionManager? _retentionManager;
    private static IReportScheduler? _reportScheduler;
    private static IEmailSender? _emailSender;
    private static OptionsForm? _optionsForm;
    private static SpeedOverlay? _speedOverlay;

    [STAThread]
    static void Main(string[] args)
    {
        // Run test mode if --test argument is passed
        if (args.Length > 0 && args[0] == "--test")
        {
            TestNetworkMonitor.Run();
            return;
        }

        // Single-instance check
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "Data Usage Reporter is already running.",
                "Already Running",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            ApplicationConfiguration.Initialize();

            // Initialize settings and database
            _settingsRepository = new SettingsRepository();
            var settings = _settingsRepository.Load();

            var dbInitializer = new DatabaseInitializer(settings.DatabasePath);
            dbInitializer.Initialize();

            // Initialize components
            _speedFormatter = new SpeedFormatter();
            _networkMonitor = new NetworkMonitor();
            _usageRepository = new UsageRepository(dbInitializer.ConnectionString);
            _usageAggregator = new UsageAggregator(_usageRepository);
            _retentionManager = new RetentionManager(_usageRepository, settings.DataRetentionDays);
            _credentialManager = new CredentialManager();
            _trayIcon = new TrayIcon(_speedFormatter);

            // Create speed overlay (visible in taskbar area)
            _speedOverlay = new SpeedOverlay(_speedFormatter);
            _speedOverlay.Show();

            // Initialize email components (supports both SMTP and direct send mode)
            var emailConfig = _settingsRepository.LoadEmailConfig();
            if (emailConfig != null && !string.IsNullOrEmpty(emailConfig.RecipientEmail))
            {
                _emailSender = new EmailSender(emailConfig, _credentialManager);
                var reportGenerator = new ReportGenerator(_usageRepository, _speedFormatter);
                _reportScheduler = new Scheduler(_settingsRepository, reportGenerator, _emailSender);
                _reportScheduler.ReportCompleted += OnReportCompleted;
                _reportScheduler.Start();
            }

            // Setup startup if configured
            var startupManager = new StartupManager();
            if (settings.StartWithWindows && !startupManager.IsStartupEnabled())
            {
                startupManager.SetStartupEnabled(true);
            }

            // Wire up tray icon events
            _trayIcon.ExitRequested += OnExitRequested;
            _trayIcon.OptionsRequested += OnOptionsRequested;

            // Handle system sleep/wake events
            SystemEvents.PowerModeChanged += OnPowerModeChanged;

            // Start monitoring timer (1-second interval)
            _monitorTimer = new System.Windows.Forms.Timer
            {
                Interval = settings.UpdateIntervalMs
            };
            _monitorTimer.Tick += OnMonitorTick;
            _monitorTimer.Start();

            // Start aggregation timer (runs every hour on the hour)
            SetupAggregationTimer();

            // Initial reading to prime the speed calculator
            _networkMonitor.GetCurrentStats();

            // Run the application
            Application.Run();
        }
        finally
        {
            Cleanup();
        }
    }

    private static void SetupAggregationTimer()
    {
        // Calculate time until next hour
        var now = DateTime.Now;
        var nextHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);
        var initialDelay = (int)(nextHour - now).TotalMilliseconds;

        _aggregationTimer = new System.Windows.Forms.Timer
        {
            Interval = Math.Max(1000, initialDelay) // At least 1 second
        };
        _aggregationTimer.Tick += OnAggregationTick;
        _aggregationTimer.Start();
    }

    private static void OnMonitorTick(object? sender, EventArgs e)
    {
        if (_networkMonitor == null || _trayIcon == null || _usageRepository == null)
            return;

        try
        {
            // Get current speed and update tray icon and overlay
            var speed = _networkMonitor.GetCurrentSpeed();
            _trayIcon.UpdateSpeed(speed);
            _speedOverlay?.UpdateSpeed(speed);

            // Get current stats and save to database
            var stats = _networkMonitor.GetCurrentStats();
            var record = new UsageRecord(
                DateTime.Now,
                stats.TotalBytesReceived,
                stats.TotalBytesSent);

            // Fire and forget - don't block the UI thread
            _ = _usageRepository.SaveRecordAsync(record);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Monitor tick error: {ex.Message}");
        }
    }

    private static async void OnAggregationTick(object? sender, EventArgs e)
    {
        if (_usageAggregator == null || _usageRepository == null || _retentionManager == null)
            return;

        try
        {
            // Aggregate the previous hour
            var previousHour = DateTime.Now.AddHours(-1);
            previousHour = new DateTime(previousHour.Year, previousHour.Month, previousHour.Day, previousHour.Hour, 0, 0);

            var summary = await _usageAggregator.AggregateHourAsync(previousHour);
            if (summary != null)
            {
                await _usageRepository.SaveSummaryAsync(summary);
            }

            // Clean up old raw records
            await _retentionManager.CleanupRawRecordsAsync();

            // Once daily, clean up old summaries (check if it's midnight hour)
            if (DateTime.Now.Hour == 0)
            {
                await _retentionManager.CleanupOldSummariesAsync();
            }

            // Reset timer to 1 hour interval after first tick
            if (_aggregationTimer != null)
            {
                _aggregationTimer.Interval = 60 * 60 * 1000; // 1 hour
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Aggregation error: {ex.Message}");
        }
    }

    private static void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        switch (e.Mode)
        {
            case PowerModes.Suspend:
                // Stop monitoring during sleep
                _monitorTimer?.Stop();
                _aggregationTimer?.Stop();
                _reportScheduler?.Stop();
                break;

            case PowerModes.Resume:
                // Resume monitoring after wake
                // Re-prime the speed calculator after wake
                _networkMonitor?.GetCurrentStats();
                _monitorTimer?.Start();

                // Recalculate next aggregation time
                _aggregationTimer?.Stop();
                SetupAggregationTimer();

                // Restart scheduler (recalculates next run)
                if (_reportScheduler is Scheduler scheduler)
                {
                    scheduler.RecalculateNextRun();
                }
                break;
        }
    }

    private static void OnReportCompleted(object? sender, ReportEventArgs e)
    {
        if (_trayIcon == null) return;

        if (e.Success)
        {
            _trayIcon.ShowNotification(
                "Report Sent",
                "Usage report has been sent successfully.",
                NotificationType.Info);
        }
        else
        {
            _trayIcon.ShowNotification(
                "Report Failed",
                e.ErrorMessage ?? "Failed to send usage report.",
                NotificationType.Error);
        }
    }

    private static void OnOptionsRequested(object? sender, EventArgs e)
    {
        if (_optionsForm == null || _optionsForm.IsDisposed)
        {
            _optionsForm = new OptionsForm(
                _usageAggregator!,
                _speedFormatter!,
                _settingsRepository!,
                _credentialManager,
                _emailSender,
                _usageRepository,
                _reportScheduler);
        }

        _optionsForm.RefreshGraph();
        _optionsForm.Show();
        _optionsForm.BringToFront();
    }

    private static void OnExitRequested(object? sender, EventArgs e)
    {
        Cleanup();
        Application.Exit();
    }

    private static void Cleanup()
    {
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;

        _monitorTimer?.Stop();
        _monitorTimer?.Dispose();
        _monitorTimer = null;

        _aggregationTimer?.Stop();
        _aggregationTimer?.Dispose();
        _aggregationTimer = null;

        _reportScheduler?.Stop();

        _optionsForm?.Dispose();
        _optionsForm = null;

        _speedOverlay?.Close();
        _speedOverlay?.Dispose();
        _speedOverlay = null;

        _trayIcon?.Dispose();
        _trayIcon = null;

        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        _mutex = null;
    }
}
