using System.Globalization;
using System.Resources;

namespace DataUsageReporter.Core.Localization;

/// <summary>
/// Service for managing application localization using .NET resource files.
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ResourceManager _resourceManager;
    private readonly Action<string> _saveLanguagePreference;
    private CultureInfo _currentCulture;

    public string CurrentLanguage => _currentCulture.TwoLetterISOLanguageName;

    public IReadOnlyList<SupportedLanguage> SupportedLanguages { get; } = new List<SupportedLanguage>
    {
        new("en", "English", new CultureInfo("en")),
        new("fr", "Français", new CultureInfo("fr"))
    };

    public event EventHandler<LanguageChangedEventArgs>? LanguageChanged;

    public LocalizationService(string? savedLanguage, Action<string> saveLanguagePreference)
    {
        _saveLanguagePreference = saveLanguagePreference;
        _resourceManager = new ResourceManager(
            "DataUsageReporter.Resources.Strings",
            typeof(LocalizationService).Assembly);

        // Initialize with saved language, OS language, or fallback to English
        var initialLanguage = savedLanguage ?? DetectOsLanguage();
        _currentCulture = GetCultureForLanguage(initialLanguage);

        // Apply culture to current thread
        Thread.CurrentThread.CurrentUICulture = _currentCulture;
    }

    public void SetLanguage(string languageCode)
    {
        if (languageCode == CurrentLanguage)
            return;

        var oldLanguage = CurrentLanguage;
        _currentCulture = GetCultureForLanguage(languageCode);

        // Apply culture to current thread
        Thread.CurrentThread.CurrentUICulture = _currentCulture;

        // Persist preference
        _saveLanguagePreference(languageCode);

        // Notify subscribers
        LanguageChanged?.Invoke(this, new LanguageChangedEventArgs(oldLanguage, languageCode));
    }

    public string GetString(string key)
    {
        try
        {
            var value = _resourceManager.GetString(key, _currentCulture);
            return value ?? key;
        }
        catch
        {
            return key;
        }
    }

    public string GetString(string key, params object[] args)
    {
        var format = GetString(key);
        try
        {
            return string.Format(_currentCulture, format, args);
        }
        catch
        {
            return format;
        }
    }

    public string FormatDate(DateTime date, string format)
    {
        return date.ToString(format, _currentCulture);
    }

    /// <summary>
    /// Detects the operating system's UI language.
    /// </summary>
    public string DetectOsLanguage()
    {
        var osLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        // Check if the OS language is supported
        if (SupportedLanguages.Any(l => l.Code == osLanguage))
        {
            return osLanguage;
        }

        // Fallback to English
        return "en";
    }

    private CultureInfo GetCultureForLanguage(string languageCode)
    {
        var supported = SupportedLanguages.FirstOrDefault(l => l.Code == languageCode);
        return supported?.Culture ?? new CultureInfo("en");
    }
}
