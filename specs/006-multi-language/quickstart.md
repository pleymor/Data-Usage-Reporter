# Quickstart: Multi-Language Support

**Feature**: 006-multi-language
**Date**: 2025-12-20

## Overview

This feature adds English and French language support to the application UI and email reports.

## Prerequisites

- .NET 8.0 SDK
- Windows 10/11 (64-bit)
- Visual Studio 2022 or VS Code with C# extension

## Build & Run

```powershell
# Clone and navigate to repo
cd C:\Users\pleym\Projects\data-usage-reporter

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run the application
dotnet run --project src\DataUsageReporter\DataUsageReporter.csproj
```

## Verify Multi-Language Support

### Test 1: Language Selection

1. Run the application (appears in system tray)
2. Double-click tray icon to open Options dialog
3. Navigate to "Settings" tab
4. Find the "Language" dropdown
5. Select "Français"
6. Verify:
   - All tab names change to French
   - All labels and buttons update immediately
   - No restart required

### Test 2: Language Persistence

1. Set language to French
2. Close the application
3. Reopen the application
4. Verify the application opens in French

### Test 3: OS Language Detection (First Run)

1. Delete `%APPDATA%\DataUsageReporter\settings.json`
2. Ensure Windows is set to French (or change temporarily)
3. Start the application
4. Verify it opens in French

### Test 4: Email in Preferred Language

1. Set language to French in Settings
2. Configure email settings (if not already)
3. Trigger a test email or wait for scheduled report
4. Verify email content is in French

## Key Files

| File | Purpose |
|------|---------|
| `src/DataUsageReporter/Resources/Strings.resx` | English strings (default) |
| `src/DataUsageReporter/Resources/Strings.fr.resx` | French strings |
| `src/DataUsageReporter/Core/Localization/ILocalizationService.cs` | Localization interface |
| `src/DataUsageReporter/Core/Localization/LocalizationService.cs` | Localization implementation |
| `src/DataUsageReporter/UI/SettingsTab.cs` | Language dropdown |

## Adding a New String

1. Add key to `Strings.resx` with English value
2. Add same key to `Strings.fr.resx` with French value
3. Use in code: `_localization.GetString("YourKey")`

## Testing

```powershell
# Run tests
dotnet test tests\DataUsageReporter.Tests\DataUsageReporter.Tests.csproj
```

## Troubleshooting

**Language doesn't change immediately?**
- Ensure UI components subscribe to `LanguageChanged` event
- Check that `RefreshStrings()` is called on event

**Missing translation shows key instead of text?**
- Verify key exists in both .resx files
- Check for typos in key names
- Rebuild to regenerate resource classes
