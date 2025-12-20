# Implementation Plan: Unified Graph

**Branch**: `005-unified-graph` | **Date**: 2025-12-20 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/005-unified-graph/spec.md`

## Summary

Simplify the graph visualization by consolidating all network usage data (download and upload) into a single unified graph. The current implementation already displays both data series on one graph with distinct colors and a legend. This feature confirms and documents the single-graph approach as the canonical design, ensuring no additional graphs are introduced.

## Technical Context

**Language/Version**: C# / .NET 8.0
**Primary Dependencies**: ScottPlot.WinForms 5.1.57, Windows Forms
**Storage**: SQLite (via Microsoft.Data.Sqlite) for usage data
**Testing**: MSTest (tests/DataUsageReporter.Tests)
**Target Platform**: Windows 10/11 (64-bit), .NET 8.0 Windows
**Project Type**: Single desktop application
**Performance Goals**: Graph updates within 1 second of time range change
**Constraints**: <100MB memory, <10% CPU idle, single executable deployment
**Scale/Scope**: Local desktop app, single user, historical data up to 5 years

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Lightweight First | ✅ Pass | No new dependencies; uses existing ScottPlot |
| II. Windows Native | ✅ Pass | Windows Forms, native Windows paths |
| III. Resource Efficiency | ✅ Pass | Single graph reduces rendering overhead vs multiple |
| IV. Simplicity | ✅ Pass | Single graph is simpler than multiple graph views |
| V. Test Discipline | ✅ Pass | GraphPanel testable via existing test infrastructure |

**Gate Result**: All principles satisfied. Proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/005-unified-graph/
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
│   ├── IUsageAggregator.cs    # Data aggregation interface
│   ├── UsageAggregator.cs     # Aggregation implementation
│   ├── AggregatorTypes.cs     # TimeGranularity, UsageDataPoint
│   ├── ISpeedFormatter.cs     # Speed formatting interface
│   └── SpeedFormatter.cs      # Mbps/Kbps formatting
├── Data/
│   ├── IUsageRepository.cs    # Data access interface
│   ├── UsageRepository.cs     # SQLite implementation
│   └── Models/
│       ├── UsageRecord.cs     # Raw usage record
│       └── UsageSummary.cs    # Aggregated summary
├── UI/
│   ├── GraphPanel.cs          # Unified graph component (primary target)
│   ├── OptionsForm.cs         # Settings dialog with graph tab
│   └── ...
└── Program.cs                 # Application entry point

tests/DataUsageReporter.Tests/
└── ...                        # Unit tests
```

**Structure Decision**: Single project structure. All graph-related changes are localized to `UI/GraphPanel.cs` with data sourced from existing `Core/UsageAggregator.cs`.

## Complexity Tracking

No violations to track. This feature simplifies rather than adds complexity.
