# Research: Multi-Language Support

**Feature**: 006-multi-language
**Date**: 2025-12-20

## Research Summary

This feature uses standard .NET localization patterns. No external dependencies required.

## Findings

### 1. Localization Approach

**Decision**: Use .NET Resource Files (.resx) with ResourceManager

**Rationale**:
- Built into .NET, no external dependencies
- Compile-time type safety with generated accessor classes
- Automatic fallback to default culture
- Industry-standard approach for WinForms applications
- Supports satellite assemblies for language-specific resources

**Alternatives Considered**:
- JSON-based translation files: Rejected - requires parsing, no compile-time safety
- Database-stored translations: Rejected - overkill for 2 languages, adds complexity
- Third-party localization libraries (e.g., Humanizer): Rejected - unnecessary dependency

### 2. Culture Detection

**Decision**: Use CultureInfo.CurrentUICulture for OS language detection

**Rationale**:
- Built-in .NET mechanism
- Respects Windows language settings
- Automatically set by the OS on application startup

**Implementation**:
```csharp
var osLanguage = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName; // "en", "fr", etc.
```

### 3. Language Switching at Runtime

**Decision**: Use Thread.CurrentThread.CurrentUICulture combined with UI refresh

**Rationale**:
- Standard .NET pattern for runtime culture switching
- ResourceManager automatically picks up culture changes
- WinForms controls need manual refresh after culture change

**Implementation Pattern**:
1. User selects new language
2. Set CurrentUICulture to new culture
3. Save preference to settings.json
4. Refresh all visible UI controls with new strings

### 4. Resource File Organization

**Decision**: Single Strings.resx file with culture-specific variants

**Rationale**:
- Simple organization for ~100 strings
- Easy to maintain and translate
- .NET automatically selects correct file based on culture

**File Structure**:
- `Strings.resx` - English (default/fallback)
- `Strings.fr.resx` - French

### 5. Email Template Localization

**Decision**: Store email templates as resources alongside UI strings

**Rationale**:
- Consistent approach with UI localization
- Templates can include placeholders for dynamic data
- Same ResourceManager mechanism works for email content

**Template Keys**:
- `Email_Subject_DailyReport`
- `Email_Body_Greeting`
- `Email_Body_SummaryHeader`
- etc.

### 6. Settings Persistence

**Decision**: Extend existing settings.json with "language" field

**Rationale**:
- Already have settings infrastructure
- Consistent with other user preferences
- Simple string value ("en" or "fr")

## Resolved Clarifications

No NEEDS CLARIFICATION items - all technical aspects use standard .NET patterns.

## Recommendations

1. **Keep it simple**: Use standard .NET ResourceManager, no custom framework
2. **Compile-time safety**: Generate strongly-typed resource accessor class
3. **Fallback chain**: English → French falls back to English if key missing
4. **Testing**: Unit test resource loading for both cultures
