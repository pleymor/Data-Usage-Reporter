# Data Model: Multi-Language Support

**Feature**: 006-multi-language
**Date**: 2025-12-20

## Overview

The multi-language feature extends the existing settings model and introduces a localization service. No new database entities are required - translations are compiled into the application.

## Entities

### UserSettings (extended)

The existing settings.json is extended with a language preference.

| Field | Type | Description |
|-------|------|-------------|
| Language | string | Two-letter ISO language code (e.g., "en", "fr"). Defaults to OS culture or "en" |

**Location**: `settings.json` in AppData folder

**Validation Rules**:
- Must be a supported language code ("en" or "fr")
- Falls back to "en" if invalid or missing

### SupportedLanguage (runtime only)

Represents a language available in the application. Not persisted.

| Field | Type | Description |
|-------|------|-------------|
| Code | string | Two-letter ISO code (e.g., "en", "fr") |
| DisplayName | string | Localized name shown in dropdown (e.g., "English", "Français") |
| Culture | CultureInfo | .NET CultureInfo for this language |

**Available Languages**:
- `en` - English (default)
- `fr` - French (Français)

### TranslationResource (compile-time)

Translations are stored in .resx files and compiled into the assembly.

| Resource File | Culture | Description |
|---------------|---------|-------------|
| Strings.resx | en (default) | English translations (fallback) |
| Strings.fr.resx | fr | French translations |

**Key Categories**:

| Category | Prefix | Example Keys |
|----------|--------|--------------|
| UI Labels | `Label_` | `Label_Download`, `Label_Upload`, `Label_Settings` |
| Buttons | `Button_` | `Button_Save`, `Button_Cancel`, `Button_Refresh` |
| Messages | `Message_` | `Message_NoData`, `Message_Error`, `Message_Success` |
| Tooltips | `Tooltip_` | `Tooltip_Graph`, `Tooltip_Tray` |
| Menu Items | `Menu_` | `Menu_Options`, `Menu_Exit`, `Menu_About` |
| Email | `Email_` | `Email_Subject`, `Email_Greeting`, `Email_Summary` |
| Tab Names | `Tab_` | `Tab_Usage`, `Tab_Graph`, `Tab_Settings`, `Tab_Credits` |

## Data Flow

```text
Application Start
     │
     ▼
Load settings.json
     │
     ├─► Language field exists → Use saved language
     │
     └─► No language field → Detect OS language (CultureInfo.CurrentUICulture)
              │
              ├─► Supported → Use OS language, save to settings
              │
              └─► Not supported → Use "en", save to settings
     │
     ▼
Set Thread.CurrentThread.CurrentUICulture
     │
     ▼
ResourceManager loads appropriate .resx file
     │
     ▼
UI controls display localized strings
```

## State Transitions

### Language Change Flow

```text
Current Language: en
     │
     ▼
User selects "Français" in Settings dropdown
     │
     ▼
LocalizationService.SetLanguage("fr")
     │
     ├─► Update CurrentUICulture
     ├─► Save to settings.json
     └─► Raise LanguageChanged event
     │
     ▼
UI components receive event
     │
     ▼
All visible text refreshes to French
     │
     ▼
Current Language: fr
```

## No Database Changes

This feature does not modify the SQLite database schema. All localization data is:
- Compiled into the application (resource files)
- Stored in user settings (language preference)
