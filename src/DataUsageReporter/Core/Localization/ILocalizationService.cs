namespace DataUsageReporter.Core.Localization;

/// <summary>
/// Service for managing application localization.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the current language code (e.g., "en", "fr").
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Gets all supported languages.
    /// </summary>
    IReadOnlyList<SupportedLanguage> SupportedLanguages { get; }

    /// <summary>
    /// Sets the application language and persists the preference.
    /// </summary>
    /// <param name="languageCode">Two-letter ISO language code</param>
    void SetLanguage(string languageCode);

    /// <summary>
    /// Gets a localized string by key.
    /// </summary>
    /// <param name="key">Resource key (e.g., "Label_Download")</param>
    /// <returns>Localized string, or key if not found</returns>
    string GetString(string key);

    /// <summary>
    /// Gets a localized string with format parameters.
    /// </summary>
    /// <param name="key">Resource key</param>
    /// <param name="args">Format arguments</param>
    /// <returns>Formatted localized string</returns>
    string GetString(string key, params object[] args);

    /// <summary>
    /// Formats a date according to the current culture.
    /// </summary>
    /// <param name="date">Date to format</param>
    /// <param name="format">Format string (culture-aware)</param>
    /// <returns>Formatted date string</returns>
    string FormatDate(DateTime date, string format);

    /// <summary>
    /// Raised when the language changes.
    /// </summary>
    event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
}
