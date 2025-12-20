# Implementation Plan: Network Usage Monitor

**Branch**: `001-network-usage-monitor` | **Date**: 2025-12-20 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-network-usage-monitor/spec.md`

## Summary

Build a lightweight Windows system tray application that monitors network data usage in real-time, displays download/upload speeds with 1-second updates, provides historical usage graphs at multiple time granularities (year/month/day/hour/minute), and supports scheduled email reports. The solution uses C# .NET 8 with WinForms for native Windows integration, SQLite for efficient historical data storage, and the IP Helper API for low-overhead network statistics.

## Technical Context

**Language/Version**: C# .NET 8.0 with WinForms
**Primary Dependencies**:
- Vanara.PInvoke.IpHlpApi (network statistics via IP Helper API)
- Microsoft.Data.Sqlite (local storage)
- ScottPlot.WinForms (charting)
- MailKit (SMTP email)

**Storage**: SQLite with WAL mode (file in %AppData%)
**Testing**: xUnit with Moq
**Target Platform**: Windows 10/11 (64-bit)
**Project Type**: Single project (Windows desktop application)
**Performance Goals**:
- 1-second update interval for real-time display
- <500ms graph rendering
- <1 second historical data queries

**Constraints**:
- <50MB memory (spec SC-002)
- <5% CPU idle (spec SC-003)
- <2s startup (spec SC-004)
- <10MB storage growth per month (spec SC-010)
- No administrator privileges required
- Single executable distribution preferred

**Scale/Scope**: Single-user desktop application, 1 year data retention

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| **I. Lightweight First** | ✅ PASS | Single executable via PublishSingleFile+Trimmed (~15-25MB). Dependencies justified: Vanara (P/Invoke wrappers), SQLite (storage), ScottPlot (graphs), MailKit (email) - all essential, no alternatives in stdlib. |
| **II. Windows Native** | ✅ PASS | C# .NET 8 WinForms runs natively on Windows. NotifyIcon for system tray is built-in. IP Helper API via P/Invoke is Windows-native. SQLite file stored in %AppData%. |
| **III. Resource Efficiency** | ✅ PASS | Target <50MB RAM (WinForms baseline ~8MB + overhead). 1-second Timer with IP Helper polling uses <1% CPU. SQLite with hourly aggregation keeps storage <10MB/month. Startup <500ms with trimmed publish. |
| **IV. Simplicity** | ✅ PASS | Single project structure. No abstraction layers - direct calls to IP Helper, SQLite, and SMTP. WinForms for UI (simplest Windows GUI). No frameworks beyond essential libraries. |
| **V. Test Discipline** | ✅ PASS | xUnit tests for core logic. Network monitoring, data aggregation, and report generation are testable without external services. SQLite in-memory mode for tests. |

**Platform Constraints Check**:
- Target OS: Windows 10/11 (64-bit) ✅
- Runtime: .NET 8 self-contained (no external runtime) ✅
- Permissions: IP Helper API read operations don't require admin ✅
- Storage: %AppData%\DataUsageReporter\ ✅
- Encoding: .NET handles UTF-8 natively ✅

## Project Structure

### Documentation (this feature)

```text
specs/001-network-usage-monitor/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (internal contracts, no REST API)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── Core/
│   ├── NetworkMonitor.cs       # IP Helper API wrapper, speed calculation
│   ├── UsageAggregator.cs      # Aggregates raw data into summaries
│   └── SpeedFormatter.cs       # B/s, KB/s, MB/s, GB/s formatting
├── Data/
│   ├── UsageRepository.cs      # SQLite data access
│   ├── SettingsRepository.cs   # App settings persistence
│   └── Migrations/             # Schema migrations
├── Email/
│   ├── ReportGenerator.cs      # Usage report HTML/text generation
│   ├── EmailSender.cs          # SMTP via MailKit
│   └── Scheduler.cs            # Timer-based report scheduling
├── UI/
│   ├── TrayIcon.cs             # NotifyIcon with context menu
│   ├── SpeedDisplay.cs         # Dual-line speed text rendering
│   ├── OptionsForm.cs          # Main options panel
│   └── GraphPanel.cs           # ScottPlot usage graphs
├── Program.cs                  # Entry point, single-instance check
└── App.config                  # Default configuration

tests/
├── Core/
│   ├── NetworkMonitorTests.cs
│   ├── UsageAggregatorTests.cs
│   └── SpeedFormatterTests.cs
├── Data/
│   └── UsageRepositoryTests.cs
├── Email/
│   ├── ReportGeneratorTests.cs
│   └── SchedulerTests.cs
└── TestHelpers/
    └── InMemoryDatabase.cs
```

**Structure Decision**: Single project structure selected. This is a standalone desktop application with no web/mobile components. All functionality is contained in one executable with clear separation by namespace (Core, Data, Email, UI).

## Complexity Tracking

> No violations - all constitution principles pass without exceptions.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| *None* | - | - |
