# Data Model: Unified Graph

**Feature**: 005-unified-graph
**Date**: 2025-12-20

## Overview

The unified graph feature uses existing data entities from the core domain model. No new entities are required.

## Entities

### UsageDataPoint (existing)

A single aggregated data point for graph display, containing both download and upload measurements for a specific timestamp.

| Field | Type | Description |
|-------|------|-------------|
| Timestamp | DateTime | The time bucket for this data point |
| DownloadBytes | long | Total bytes downloaded in this period |
| UploadBytes | long | Total bytes uploaded in this period |

**Location**: `src/DataUsageReporter/Core/AggregatorTypes.cs`

**Validation Rules**:
- Timestamp must be valid (not default)
- DownloadBytes >= 0
- UploadBytes >= 0

### TimeGranularity (existing)

Enumeration defining the aggregation level for graph data.

| Value | Description |
|-------|-------------|
| Minute | Per-minute aggregation (last 60 minutes view) |
| Hour | Per-hour aggregation (last 24 hours view) |
| Day | Per-day aggregation (last 30 days view) |
| Month | Per-month aggregation (last 12 months view) |
| Year | Per-year aggregation (last 5 years view) |

**Location**: `src/DataUsageReporter/Core/AggregatorTypes.cs`

## Data Flow

```text
SQLite Database
     │
     ▼
UsageRepository.GetSummariesAsync()
     │
     ▼
UsageAggregator.GetDataPointsAsync(from, to, granularity)
     │
     ▼
List<UsageDataPoint>
     │
     ▼
GraphPanel.UpdatePlot(dataPoints)
     │
     ▼
ScottPlot renders unified graph with:
  - Download line (green, #4CAF50)
  - Upload line (orange, #FF9800)
  - Legend (upper right)
  - Y-axis: Mbps
  - X-axis: DateTime
```

## Unit Conversion

The graph converts bytes to Mbps for display:

```text
Mbps = bytes × 8 / 1,000,000
```

This conversion happens in `GraphPanel.UpdatePlot()` before rendering.

## State Transitions

N/A - This feature displays read-only historical data. No state transitions occur.

## Relationships

```text
UsageRecord (1) ──aggregates──▶ (N) UsageDataPoint
                     via
              UsageAggregator
```

## No Schema Changes Required

The unified graph uses existing data structures. No database migrations or entity changes needed.
