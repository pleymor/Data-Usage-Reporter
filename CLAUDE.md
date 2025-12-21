# data-usage-reporter Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-12-20

## Active Technologies
- C# .NET 8.0 + WinForms (existing), System.Reflection (for version info) (002-credits-tab)
- N/A (static content, no persistence needed) (002-credits-tab)
- C# .NET 8.0 + WinForms, Vanara.PInvoke.IpHlpApi, Microsoft.Data.Sqlite, ScottPlot (003-filter-spike-values)
- SQLite (usage.db in AppData) (003-filter-spike-values)
- C# / .NET 8.0 + ScottPlot.WinForms 5.1.57, Windows Forms (005-unified-graph)
- SQLite (via Microsoft.Data.Sqlite) for usage data (005-unified-graph)
- C# / .NET 8.0 + .NET Resource files (.resx), System.Globalization (006-multi-language)
- Language preference stored in settings.json (006-multi-language)
- C# .NET 8.0 Windows Forms + ScottPlot.WinForms 5.1.57 (graph rendering), MailKit 4.14.1 (email with inline images) (007-enhanced-email-reports)
- SQLite via Microsoft.Data.Sqlite (hourly UsageSummary records) (007-enhanced-email-reports)
- C# .NET 8.0 + ScottPlot.WinForms 5.1.57, Microsoft.Data.Sqlite (008-extended-graph-history)
- SQLite (usage.db in AppData) - `usage_records` (raw, temporary) and `usage_summaries` (aggregated, persistent) (008-extended-graph-history)

- C# .NET 8.0 with WinForms (001-network-usage-monitor)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# .NET 8.0 with WinForms

## Code Style

C# .NET 8.0 with WinForms: Follow standard conventions

## Recent Changes
- 008-extended-graph-history: Added C# .NET 8.0 + ScottPlot.WinForms 5.1.57, Microsoft.Data.Sqlite
- 007-enhanced-email-reports: Added C# .NET 8.0 Windows Forms + ScottPlot.WinForms 5.1.57 (graph rendering), MailKit 4.14.1 (email with inline images)
- 006-multi-language: Added C# / .NET 8.0 + .NET Resource files (.resx), System.Globalization


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
