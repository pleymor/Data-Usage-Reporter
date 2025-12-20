# Research: Unified Graph

**Feature**: 005-unified-graph
**Date**: 2025-12-20

## Research Summary

This feature has minimal unknowns since the existing implementation already follows the unified graph pattern. Research focused on confirming current approach and identifying any improvements.

## Findings

### 1. Current Graph Implementation

**Decision**: Retain current single-graph approach with ScottPlot

**Rationale**: The existing `GraphPanel.cs` implementation already:
- Displays both download and upload on a single graph
- Uses distinct colors (green for download, orange for upload)
- Provides time granularity selection (minute, hour, day, month, year)
- Shows a legend identifying each data series
- Handles empty data states with "No data available" message

**Alternatives Considered**:
- Separate graphs for download/upload: Rejected - more complex, requires more screen space, harder to compare data
- Stacked area chart: Rejected - obscures individual values, current line chart is clearer
- Bar charts: Rejected - less suitable for time series data with many points

### 2. ScottPlot Integration

**Decision**: Continue using ScottPlot.WinForms 5.1.57

**Rationale**:
- Already integrated and working
- Provides all required features (scatter/line plots, legends, axes formatting)
- Lightweight, no additional runtime dependencies
- Good performance for interactive graphs

**Alternatives Considered**:
- LiveCharts2: More features but heavier dependency
- OxyPlot: Similar capabilities but would require migration effort
- Custom GDI+ drawing: More control but significantly more development effort

### 3. Data Flow

**Decision**: Use existing `IUsageAggregator.GetDataPointsAsync()` pattern

**Rationale**:
- Already provides aggregated data points with download/upload bytes
- Supports all required time granularities
- Async pattern prevents UI blocking

**No changes required to data layer.**

### 4. Color Scheme

**Decision**: Keep current color scheme (green download, orange upload)

**Rationale**:
- Standard convention (download = green/down, upload = orange/up)
- Good contrast between the two colors
- Accessible for most color vision deficiencies

### 5. Performance Considerations

**Decision**: Current implementation meets performance targets

**Rationale**:
- Graph updates are async and non-blocking
- ScottPlot handles rendering efficiently
- Data aggregation is performed at database level via `UsageAggregator`

**Measurements needed**: None - current implementation is performant

## Resolved Clarifications

All technical unknowns resolved. No NEEDS CLARIFICATION items remained after spec review.

## Recommendations

1. **No code changes required** - current implementation satisfies all spec requirements
2. **Documentation update** - confirm single-graph as canonical approach in code comments
3. **Testing** - add/verify tests for graph rendering edge cases (empty data, single series)
