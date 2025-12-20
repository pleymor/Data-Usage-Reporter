# Quickstart: Unified Graph

**Feature**: 005-unified-graph
**Date**: 2025-12-20

## Overview

This feature confirms the single unified graph as the canonical visualization approach. The current implementation already satisfies all requirements.

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

## Verify Unified Graph

1. Run the application (appears in system tray)
2. Double-click tray icon to open Options dialog
3. Navigate to "Usage Graph" tab
4. Verify:
   - Single graph displays both download (green) and upload (orange) lines
   - Legend in upper-right identifies both series
   - Time range dropdown changes graph period
   - Y-axis shows Mbps, X-axis shows time

## Key Files

| File | Purpose |
|------|---------|
| `src/DataUsageReporter/UI/GraphPanel.cs` | Unified graph component |
| `src/DataUsageReporter/UI/OptionsForm.cs` | Host dialog with Usage Graph tab |
| `src/DataUsageReporter/Core/UsageAggregator.cs` | Data aggregation for graph |
| `src/DataUsageReporter/Core/AggregatorTypes.cs` | UsageDataPoint, TimeGranularity |

## Testing

```powershell
# Run tests
dotnet test tests\DataUsageReporter.Tests\DataUsageReporter.Tests.csproj
```

## What This Feature Changes

**Minimal to no code changes required** - the current implementation already:
- Displays a single unified graph
- Shows both download and upload data
- Uses distinct colors with a legend
- Supports all time granularities

The primary deliverable is documentation confirming this as the canonical design.

## Next Steps

After `/speckit.tasks`:
1. Review existing GraphPanel implementation against spec
2. Add any missing tests for edge cases
3. Update code comments to document single-graph design decision
