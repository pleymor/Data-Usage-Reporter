# Implementation Plan: Graph Shows All Historical Data (Bug Fix)

**Branch**: `008-extended-graph-history` | **Date**: 2025-12-21 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/008-extended-graph-history/spec.md`

## Summary

The graph currently only shows today's data because it uses `TimeGranularity.Minute` which queries raw `usage_records`. These raw records are deleted after being aggregated into hourly `usage_summaries`. The fix is to change the graph to use hourly summaries (`TimeGranularity.Hour`) which contain all historical data.

## Technical Context

**Language/Version**: C# .NET 8.0
**Primary Dependencies**: ScottPlot.WinForms 5.1.57, Microsoft.Data.Sqlite
**Storage**: SQLite (usage.db in AppData) - `usage_records` (raw, temporary) and `usage_summaries` (aggregated, persistent)
**Testing**: N/A (existing test patterns)
**Target Platform**: Windows 10/11 (64-bit)
**Project Type**: Single WinForms application
**Performance Goals**: Graph must render quickly with up to 5 years of hourly data (~43,800 data points max)
**Constraints**: Peak memory < 100MB, startup < 2 seconds
**Scale/Scope**: Single user, local data only

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Lightweight First | PASS | No new dependencies, single-line fix |
| II. Windows Native | PASS | No changes to platform compatibility |
| III. Resource Efficiency | PASS | Hourly data (~43,800 points max) is smaller than minute data |
| IV. Simplicity | PASS | Minimal code change - just change granularity parameter |
| V. Test Discipline | PASS | Existing test patterns apply |

## Project Structure

### Documentation (this feature)

```text
specs/008-extended-graph-history/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── spec.md              # Feature specification
└── checklists/
    └── requirements.md  # Quality checklist
```

### Source Code (repository root)

```text
src/DataUsageReporter/
├── Core/
│   └── UsageAggregator.cs    # Already supports Hour granularity
├── Data/
│   └── UsageRepository.cs    # GetSummariesAsync already works correctly
└── UI/
    └── GraphPanel.cs         # FIX: Change from Minute to Hour granularity
```

**Structure Decision**: Single file change in existing structure. No new files needed.

## Root Cause Analysis

**Problem**: GraphPanel.cs line 67 uses `TimeGranularity.Minute`:
```csharp
var dataPoints = await _aggregator.GetDataPointsAsync(from, to, TimeGranularity.Minute);
```

**Why this fails**:
1. `TimeGranularity.Minute` calls `GetMinuteDataPointsAsync()`
2. This queries `usage_records` table (raw per-second samples)
3. Raw records are periodically deleted after being aggregated into `usage_summaries`
4. Only today's raw records exist; historical data is only in `usage_summaries`

**Solution**: Change to `TimeGranularity.Hour` which:
1. Queries `usage_summaries` table via `GetSummariesAsync()`
2. Returns all historical hourly aggregates
3. Provides complete data history

## Implementation

### Change Required

**File**: `src/DataUsageReporter/UI/GraphPanel.cs`
**Line**: 67
**Change**: `TimeGranularity.Minute` → `TimeGranularity.Hour`

```csharp
// Before:
var dataPoints = await _aggregator.GetDataPointsAsync(from, to, TimeGranularity.Minute);

// After:
var dataPoints = await _aggregator.GetDataPointsAsync(from, to, TimeGranularity.Hour);
```

### Verification

1. Build the application
2. Open Options → Usage Graph tab
3. Verify graph shows data from previous days (if available in database)
4. Verify X-axis displays date range spanning multiple days

## Complexity Tracking

No violations - this is a minimal bug fix.
