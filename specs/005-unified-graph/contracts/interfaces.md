# Contracts: Unified Graph

**Feature**: 005-unified-graph
**Date**: 2025-12-20

## Overview

The unified graph feature relies on existing interfaces. No new APIs or contracts are required. This document describes the existing contracts that the graph component uses.

## Existing Interfaces

### IUsageAggregator

Provides aggregated data points for graph rendering.

**Location**: `src/DataUsageReporter/Core/IUsageAggregator.cs`

```csharp
public interface IUsageAggregator
{
    /// <summary>
    /// Aggregates summaries for display at different granularities.
    /// </summary>
    Task<IReadOnlyList<UsageDataPoint>> GetDataPointsAsync(
        DateTime from,
        DateTime to,
        TimeGranularity granularity
    );

    // Other methods not used by unified graph...
}
```

**Usage by GraphPanel**:
- Called in `RefreshDataAsync()` when time range changes
- Returns data points covering the selected time range
- Granularity maps to dropdown selection:
  - "Last 60 Minutes" → `TimeGranularity.Minute`
  - "Last 24 Hours" → `TimeGranularity.Hour`
  - "Last 30 Days" → `TimeGranularity.Day`
  - "Last 12 Months" → `TimeGranularity.Month`
  - "Last 5 Years" → `TimeGranularity.Year`

### ISpeedFormatter

Formats speed values for display (used in tooltips/labels if extended).

**Location**: `src/DataUsageReporter/Core/ISpeedFormatter.cs`

```csharp
public interface ISpeedFormatter
{
    string FormatSpeed(long bytesPerSecond);
    string FormatBytes(long bytes);
}
```

**Current Usage**: Injected into GraphPanel but not currently used for graph rendering (Y-axis shows raw Mbps). Available for future tooltip enhancements.

## GraphPanel Public Interface

The `GraphPanel` class exposes the following public members:

```csharp
public class GraphPanel : UserControl
{
    // Constructor - receives dependencies
    public GraphPanel(IUsageAggregator aggregator, ISpeedFormatter formatter);

    // Public method to refresh graph data
    public Task RefreshDataAsync();
}
```

**Integration Point**: `OptionsForm` hosts `GraphPanel` in the "Usage Graph" tab and can call `RefreshDataAsync()` to update the display.

## No New Contracts Required

The unified graph feature:
- Uses existing `IUsageAggregator` for data retrieval
- Uses existing `UsageDataPoint` record for data transfer
- Renders data using ScottPlot (external library)

No additional interfaces, DTOs, or API contracts are needed for this feature.
