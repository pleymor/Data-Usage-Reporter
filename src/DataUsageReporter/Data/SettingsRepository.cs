using System.Text.Json;

namespace DataUsageReporter.Data;

/// <summary>
/// Persists application settings to %AppData%\DataUsageReporter\settings.json.
/// </summary>
public class SettingsRepository : ISettingsRepository
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    private class SettingsFile
    {
        public AppSettings? AppSettings { get; set; }
        public EmailConfig? EmailConfig { get; set; }
        public ReportSchedule? ReportSchedule { get; set; }
    }

    public SettingsRepository()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "DataUsageReporter");

        Directory.CreateDirectory(appDataPath);
        _settingsPath = Path.Combine(appDataPath, "settings.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public AppSettings Load()
    {
        var file = LoadFile();
        return file.AppSettings ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        var file = LoadFile();
        file.AppSettings = settings;
        SaveFile(file);
    }

    public EmailConfig? LoadEmailConfig()
    {
        var file = LoadFile();
        return file.EmailConfig;
    }

    public void SaveEmailConfig(EmailConfig config)
    {
        var file = LoadFile();
        file.EmailConfig = config;
        SaveFile(file);
    }

    public ReportSchedule? LoadSchedule()
    {
        var file = LoadFile();
        return file.ReportSchedule;
    }

    public void SaveSchedule(ReportSchedule schedule)
    {
        var file = LoadFile();
        file.ReportSchedule = schedule;
        SaveFile(file);
    }

    private SettingsFile LoadFile()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new SettingsFile();

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<SettingsFile>(json, _jsonOptions) ?? new SettingsFile();
        }
        catch
        {
            // Handle missing/corrupt files by returning defaults
            return new SettingsFile();
        }
    }

    private void SaveFile(SettingsFile file)
    {
        var json = JsonSerializer.Serialize(file, _jsonOptions);
        File.WriteAllText(_settingsPath, json);
    }
}
