using System.Globalization;

namespace DataUsageReporter.Core.Localization;

/// <summary>
/// Represents a language supported by the application.
/// </summary>
public record SupportedLanguage(
    string Code,
    string DisplayName,
    CultureInfo Culture
);
