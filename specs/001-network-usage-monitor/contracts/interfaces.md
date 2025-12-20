# Internal Contracts: Network Usage Monitor

**Date**: 2025-12-20
**Feature**: 001-network-usage-monitor

This document defines the internal interfaces between components. Since this is a desktop application without external APIs, these contracts ensure consistent component integration.

## Core Interfaces

### INetworkMonitor

Responsible for reading network statistics from the operating system.

```csharp
public interface INetworkMonitor
{
    /// <summary>
    /// Gets current network statistics for all physical adapters.
    /// </summary>
    /// <returns>Aggregate statistics across all adapters</returns>
    NetworkStats GetCurrentStats();

    /// <summary>
    /// Gets the current download and upload speeds.
    /// </summary>
    /// <returns>Speed in bytes per second</returns>
    SpeedReading GetCurrentSpeed();
}

public record NetworkStats(
    long TotalBytesReceived,
    long TotalBytesSent,
    DateTime Timestamp
);

public record SpeedReading(
    long DownloadBytesPerSecond,
    long UploadBytesPerSecond,
    DateTime Timestamp
);
```

**Contract Rules**:
- MUST return aggregate of all physical network adapters
- MUST NOT include virtual adapters (VPN tunnels counted at physical level)
- MUST handle adapter disconnection gracefully (return last known values)
- MUST be thread-safe for 1-second polling

### IUsageRepository

Data access for usage records and summaries.

```csharp
public interface IUsageRepository
{
    // Raw records (temporary storage)
    Task SaveRecordAsync(UsageRecord record);
    Task<IReadOnlyList<UsageRecord>> GetRecordsSinceAsync(DateTime since);
    Task DeleteRecordsBeforeAsync(DateTime before);

    // Aggregated summaries (long-term storage)
    Task SaveSummaryAsync(UsageSummary summary);
    Task<IReadOnlyList<UsageSummary>> GetSummariesAsync(DateTime from, DateTime to);
    Task DeleteSummariesBeforeAsync(DateTime before);

    // Statistics
    Task<UsageSummary?> GetTotalUsageAsync(DateTime from, DateTime to);
}
```

**Contract Rules**:
- All async methods MUST complete within 1 second
- MUST handle concurrent reads during writes (WAL mode)
- MUST throw `RepositoryException` on database errors
- DateTime parameters are in local time; storage uses UTC

### IUsageAggregator

Aggregates raw records into summaries.

```csharp
public interface IUsageAggregator
{
    /// <summary>
    /// Aggregates raw records for the specified hour.
    /// </summary>
    /// <param name="hourStart">Start of the hour (truncated to hour)</param>
    /// <returns>Aggregated summary, or null if no records</returns>
    Task<UsageSummary?> AggregateHourAsync(DateTime hourStart);

    /// <summary>
    /// Aggregates summaries for display at different granularities.
    /// </summary>
    Task<IReadOnlyList<DataPoint>> GetDataPointsAsync(
        DateTime from,
        DateTime to,
        TimeGranularity granularity
    );
}

public enum TimeGranularity
{
    Minute,
    Hour,
    Day,
    Month,
    Year
}

public record DataPoint(
    DateTime Timestamp,
    long DownloadBytes,
    long UploadBytes
);
```

**Contract Rules**:
- Minute granularity uses raw records (last 60 minutes only)
- Hour+ granularity uses UsageSummary records
- MUST handle missing data points (return zero values)

### ISpeedFormatter

Formats byte counts and speeds for display.

```csharp
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
```

**Contract Rules**:
- Auto-select unit: B/s, KB/s, MB/s, GB/s based on magnitude
- Use 1024-based units (KiB) internally, display as KB/MB/GB
- Maximum 1 decimal place for readability
- Zero shown as "0 B/s" or "0 B"

## Email Interfaces

### IEmailSender

Sends email reports via SMTP.

```csharp
public interface IEmailSender
{
    /// <summary>
    /// Sends an email with the specified content.
    /// </summary>
    /// <returns>True if sent successfully</returns>
    Task<bool> SendAsync(EmailMessage message);

    /// <summary>
    /// Tests the current email configuration.
    /// </summary>
    /// <returns>Validation result with error details if failed</returns>
    Task<ValidationResult> TestConnectionAsync();
}

public record EmailMessage(
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null
);

public record ValidationResult(
    bool IsValid,
    string? ErrorMessage = null
);
```

**Contract Rules**:
- MUST use credentials from Windows Credential Manager
- MUST support TLS/SSL connections
- MUST timeout after 30 seconds
- MUST NOT block UI thread

### IReportGenerator

Generates usage report content.

```csharp
public interface IReportGenerator
{
    /// <summary>
    /// Generates a usage report for the specified period.
    /// </summary>
    Task<EmailMessage> GenerateReportAsync(
        DateTime periodStart,
        DateTime periodEnd,
        ReportFrequency frequency
    );
}

public enum ReportFrequency
{
    Daily,
    Weekly,
    Monthly
}
```

**Contract Rules**:
- Report includes: period summary, daily breakdown, peak usage times
- HTML format with inline CSS (no external resources)
- Plain text alternative for email clients

### IReportScheduler

Manages scheduled report execution.

```csharp
public interface IReportScheduler
{
    /// <summary>
    /// Starts the scheduler background service.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the scheduler.
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets the next scheduled run time.
    /// </summary>
    DateTime? GetNextRunTime();

    /// <summary>
    /// Event raised when a report is sent or fails.
    /// </summary>
    event EventHandler<ReportEventArgs> ReportCompleted;
}

public record ReportEventArgs(
    bool Success,
    DateTime Timestamp,
    string? ErrorMessage = null
);
```

**Contract Rules**:
- MUST persist schedule across application restarts
- MUST handle system sleep/wake (recalculate next run time)
- MUST notify on success or failure via event

## UI Interfaces

### ITrayIcon

Manages system tray presence and display.

```csharp
public interface ITrayIcon
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
    event EventHandler OptionsRequested;

    /// <summary>
    /// Event raised when user requests exit.
    /// </summary>
    event EventHandler ExitRequested;
}

public enum NotificationType
{
    Info,
    Warning,
    Error
}
```

**Contract Rules**:
- Speed display uses dual-line format: "↓ X.X MB/s" / "↑ X.X MB/s"
- Context menu items: Options, Exit
- Double-click opens Options panel

## Settings Interfaces

### ISettingsRepository

Persists application settings.

```csharp
public interface ISettingsRepository
{
    AppSettings Load();
    void Save(AppSettings settings);

    EmailConfig? LoadEmailConfig();
    void SaveEmailConfig(EmailConfig config);

    ReportSchedule? LoadSchedule();
    void SaveSchedule(ReportSchedule schedule);
}
```

**Contract Rules**:
- Settings file: %AppData%\DataUsageReporter\settings.json
- MUST create directory if not exists
- MUST handle missing/corrupt files (return defaults)
- MUST NOT store SMTP credentials (use Credential Manager)

### ICredentialManager

Secure credential storage wrapper.

```csharp
public interface ICredentialManager
{
    /// <summary>
    /// Stores credentials securely.
    /// </summary>
    void Store(string key, string username, string password);

    /// <summary>
    /// Retrieves stored credentials.
    /// </summary>
    (string Username, string Password)? Retrieve(string key);

    /// <summary>
    /// Deletes stored credentials.
    /// </summary>
    void Delete(string key);
}
```

**Contract Rules**:
- Uses Windows Credential Manager (target: "DataUsageReporter")
- MUST NOT log or expose passwords
- Returns null if credential not found
