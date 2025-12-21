# Contracts: Multi-Language Support

**Feature**: 006-multi-language
**Date**: 2025-12-20

## Overview

This feature introduces a localization service interface and extends the settings contract. No external APIs are involved.

## Interfaces

### ILocalizationService

Central service for managing application localization.

```csharp
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
    /// Raised when the language changes.
    /// </summary>
    event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
}
```

### SupportedLanguage

Represents a language available in the application.

```csharp
public record SupportedLanguage(
    string Code,        // "en", "fr"
    string DisplayName, // "English", "Français"
    CultureInfo Culture
);
```

### LanguageChangedEventArgs

Event args for language change notifications.

```csharp
public class LanguageChangedEventArgs : EventArgs
{
    public string OldLanguage { get; }
    public string NewLanguage { get; }
}
```

## Settings Contract Extension

The existing `ISettingsService` (or settings model) is extended:

```csharp
public interface ISettingsService
{
    // ... existing members ...

    /// <summary>
    /// Gets or sets the preferred language code.
    /// </summary>
    string Language { get; set; }
}
```

**JSON Schema** (settings.json):

```json
{
  "language": "en",
  // ... existing settings ...
}
```

## UI Contract

Each localizable UI component must:

1. Subscribe to `ILocalizationService.LanguageChanged`
2. Implement a `RefreshStrings()` method
3. Call `RefreshStrings()` on language change

Example pattern:

```csharp
public class LocalizableForm : Form
{
    private readonly ILocalizationService _localization;

    public LocalizableForm(ILocalizationService localization)
    {
        _localization = localization;
        _localization.LanguageChanged += OnLanguageChanged;
        RefreshStrings();
    }

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        RefreshStrings();
    }

    protected virtual void RefreshStrings()
    {
        // Override in derived classes to update control text
    }
}
```

## Email Contract

Email templates follow a standard format with placeholders:

| Key | English Value | French Value |
|-----|---------------|--------------|
| `Email_Subject_DailyReport` | "Daily Usage Report - {0:d}" | "Rapport d'utilisation quotidien - {0:d}" |
| `Email_Greeting` | "Hello," | "Bonjour," |
| `Email_Summary_Download` | "Downloaded: {0}" | "Téléchargé : {0}" |
| `Email_Summary_Upload` | "Uploaded: {0}" | "Téléversé : {0}" |
| `Email_Footer` | "Data Usage Reporter" | "Rapport d'utilisation des données" |

Placeholders use standard .NET format strings:
- `{0}` - First argument
- `{0:d}` - Date in short format
- `{0:N2}` - Number with 2 decimal places
