# Implementation Plan: Multi-Language Support

**Branch**: `006-multi-language` | **Date**: 2025-12-20 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/006-multi-language/spec.md`

## Summary

Add multi-language support to the application (English and French) for both the UI and email reports. Users can select their preferred language in Settings, and the choice persists across restarts. The app auto-detects OS language on first run.

## Technical Context

**Language/Version**: C# / .NET 8.0
**Primary Dependencies**: .NET Resource files (.resx), System.Globalization
**Storage**: Language preference stored in settings.json
**Testing**: MSTest (tests/DataUsageReporter.Tests)
**Target Platform**: Windows 10/11 (64-bit), .NET 8.0 Windows
**Project Type**: Single desktop application (WinForms)
**Performance Goals**: Language switch within 2 seconds, no restart required
**Constraints**: <100MB memory, <10% CPU idle, bundled translations (no runtime download)
**Scale/Scope**: 2 languages (English, French), ~50-100 UI strings, ~20 email strings

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Lightweight First | ✅ Pass | Uses built-in .NET resource system, no external dependencies |
| II. Windows Native | ✅ Pass | .NET resources are Windows-native, uses CultureInfo for OS detection |
| III. Resource Efficiency | ✅ Pass | Resource files are compiled into assembly, minimal memory overhead |
| IV. Simplicity | ✅ Pass | Standard .NET localization pattern, no custom framework |
| V. Test Discipline | ✅ Pass | Resource loading can be unit tested |

**Gate Result**: All principles satisfied. Proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/006-multi-language/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/DataUsageReporter/
├── Core/
│   └── Localization/
│       ├── ILocalizationService.cs   # Localization interface
│       └── LocalizationService.cs    # Localization implementation
├── Resources/
│   ├── Strings.resx                  # English (default) strings
│   └── Strings.fr.resx               # French strings
├── UI/
│   ├── SettingsTab.cs                # Language dropdown added here
│   └── [other UI files...]           # Updated to use localized strings
├── Email/
│   └── EmailService.cs               # Updated to use localized templates
└── Program.cs                        # Initialize localization on startup

tests/DataUsageReporter.Tests/
└── LocalizationTests.cs              # Tests for localization service
```

**Structure Decision**: Single project structure. Localization is added via a new `Core/Localization` folder and `Resources` folder. Existing UI and Email components are updated to use localized strings.

## Complexity Tracking

No violations to track. Standard .NET localization approach with minimal complexity.
