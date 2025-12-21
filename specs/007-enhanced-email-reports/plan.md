# Implementation Plan: Enhanced Email Reports with Graphs and Tables

**Branch**: `007-enhanced-email-reports` | **Date**: 2025-12-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-enhanced-email-reports/spec.md`

## Summary

Enhance email reports to include frequency-specific graphs (hourly/daily/weekly) rendered as inline images, followed by structured tables showing historical usage data with totals and peak speeds. The existing ScottPlot library will be used to generate graph images, and the current ReportGenerator will be extended to support the new format.

## Technical Context

**Language/Version**: C# .NET 8.0 Windows Forms
**Primary Dependencies**: ScottPlot.WinForms 5.1.57 (graph rendering), MailKit 4.14.1 (email with inline images)
**Storage**: SQLite via Microsoft.Data.Sqlite (hourly UsageSummary records)
**Testing**: xUnit (existing test project at tests/DataUsageReporter.Tests)
**Target Platform**: Windows 10/11 (64-bit)
**Project Type**: Single desktop application
**Performance Goals**: Report generation < 10 seconds, graph rendering < 3 seconds
**Constraints**: Peak memory < 100MB, CPU idle < 10%, per constitution
**Scale/Scope**: Up to 31 days of historical data, 24 hourly data points, 5 weekly aggregates

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Lightweight First | PASS | Uses existing ScottPlot dependency, no new external libraries needed |
| II. Windows Native | PASS | All code runs natively on Windows, no WSL/Cygwin required |
| III. Resource Efficiency | PASS | Graph rendered in-memory as PNG, streamed to email; no persistent memory overhead |
| IV. Simplicity | PASS | Extends existing ReportGenerator rather than creating new abstractions |
| V. Test Discipline | PASS | Will add unit tests for new aggregation and rendering logic |

## Project Structure

### Documentation (this feature)

```text
specs/007-enhanced-email-reports/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/DataUsageReporter/
├── Core/
│   ├── IUsageAggregator.cs      # Existing - may need Week granularity
│   ├── UsageAggregator.cs       # Existing - may need Week granularity
│   └── AggregatorTypes.cs       # Existing - TimeGranularity enum
├── Email/
│   ├── IReportGenerator.cs      # Existing interface
│   ├── ReportGenerator.cs       # Modify - add graph generation
│   ├── EmailReportGraphRenderer.cs  # NEW - renders ScottPlot to PNG
│   └── EmailSender.cs           # Existing - add inline image support
├── Data/
│   ├── IUsageRepository.cs      # Existing
│   └── Models/
│       └── UsageSummary.cs      # Existing
└── Resources/
    ├── Strings.resx             # Add new translation keys
    └── Strings.fr.resx          # Add new translation keys

tests/DataUsageReporter.Tests/
├── ReportGeneratorTests.cs      # NEW - test report generation
└── EmailReportGraphRendererTests.cs  # NEW - test graph rendering
```

**Structure Decision**: Single project structure maintained. New functionality added to existing Email/ folder with one new class (EmailReportGraphRenderer) for graph rendering responsibilities.

## Complexity Tracking

> No constitution violations. Feature uses existing patterns and dependencies.

| Aspect | Approach | Justification |
|--------|----------|---------------|
| Graph rendering | ScottPlot.Plot.SavePng() | Already in project, proven for UI graphs |
| Email embedding | MailKit LinkedResource | Standard MIME inline image approach |
| Week aggregation | Extend TimeGranularity enum | Minimal change, follows existing pattern |
