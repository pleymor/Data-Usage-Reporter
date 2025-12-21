namespace DataUsageReporter.Data;

/// <summary>
/// Persists application settings to %AppData%\DataUsageReporter\settings.json.
/// </summary>
public interface ISettingsRepository
{
    AppSettings Load();
    void Save(AppSettings settings);

    EmailConfig? LoadEmailConfig();
    void SaveEmailConfig(EmailConfig config);

    ReportSchedule? LoadSchedule();
    void SaveSchedule(ReportSchedule schedule);
}

/// <summary>
/// Application-wide settings.
/// </summary>
public class AppSettings
{
    public bool StartWithWindows { get; set; } = true;
    public int DataRetentionDays { get; set; } = 365;
    public int UpdateIntervalMs { get; set; } = 1000;
    public string DatabasePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DataUsageReporter",
        "usage.db");
    public string? Language { get; set; }
}

/// <summary>
/// SMTP configuration for email reports. Credentials stored separately in Credential Manager.
/// </summary>
public class EmailConfig
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string SenderEmail { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string CredentialKey { get; set; } = "DataUsageReporter:SMTP";
    public string? CustomSubject { get; set; }
}

/// <summary>
/// Configuration for automated email report delivery.
/// </summary>
public class ReportSchedule
{
    public ReportFrequency Frequency { get; set; } = ReportFrequency.Weekly;
    public TimeSpan TimeOfDay { get; set; } = new TimeSpan(8, 0, 0);
    public DayOfWeek? DayOfWeek { get; set; } = System.DayOfWeek.Monday;
    public int? DayOfMonth { get; set; }
    public bool IsEnabled { get; set; } = false;
    public long? LastRunTime { get; set; }
    public long NextRunTime { get; set; }
}

public enum ReportFrequency
{
    Daily,
    Weekly,
    Monthly
}
