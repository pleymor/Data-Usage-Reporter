# Research: Filter Impossible Spike Values

**Feature**: 003-filter-spike-values
**Date**: 2025-12-20

## Root Cause Analysis

### Decision: Spike Origin Identified
**Rationale**: Analysis of the codebase revealed multiple sources of spike values:

1. **Windows API Counter Jumps** (PRIMARY)
   - `GetIfTable2()` returns cumulative byte counters that can jump during adapter reconnection
   - When an adapter disconnects/reconnects, counters may reset or report stale buffered data
   - The delta calculation (`current - previous`) produces impossibly large values

2. **No Upper Bound Validation**
   - Current code only filters negative deltas (counter resets)
   - No check for impossibly high positive deltas
   - Peak speed calculations in `UsageAggregator` have no maximum threshold

3. **Double API Calls**
   - `GetIfTable2` called twice per tick without synchronization
   - Values can diverge if adapter state changes between calls

**Alternatives Considered**:
- Statistical outlier detection (median absolute deviation) - Rejected as overly complex for simple threshold filtering
- Moving average smoothing - Rejected as it would delay legitimate readings

## Filtering Strategy

### Decision: Threshold-Based Filtering with Hold Last Value
**Rationale**: Simple, predictable, O(1) complexity, easy to understand and debug.

**Implementation Approach**:
1. Define maximum speed threshold: 10 Gbps = 1,250,000,000 bytes/second
2. Compare each speed reading against threshold
3. If exceeded: return previous valid reading (hold behavior)
4. Store last valid reading in NetworkMonitor state

**Alternatives Considered**:
- Exponential moving average - Rejected: smooths legitimate readings, adds latency
- Median filter (window of N readings) - Rejected: requires buffer, O(N) memory
- Kalman filter - Rejected: overengineered for simple spike detection

## Threshold Value

### Decision: Default 10 Gbps (1.25 GB/s)
**Rationale**:
- Accommodates current fastest consumer connections (10 Gbps fiber)
- Well above typical consumer connections (1 Gbps)
- Far below obviously impossible values (100+ Gbps spikes observed)
- Configurable for users with faster connections

**Calculation**:
- 10 Gbps = 10,000,000,000 bits/second
- 10 Gbps = 1,250,000,000 bytes/second (divide by 8)
- Constant: `MaxBytesPerSecond = 1_250_000_000L`

**Alternatives Considered**:
- 1 Gbps - Rejected: too restrictive for fiber users
- 100 Gbps - Rejected: still allows unrealistic spikes
- Auto-detect from adapter speed - Rejected: adds complexity, adapter speed not always accurate

## Implementation Location

### Decision: Filter at NetworkMonitor.GetCurrentSpeed()
**Rationale**:
- Single point of filtering prevents corrupted data from propagating
- Upstream of storage (no bad data enters database)
- Upstream of display (no spike shown to user)
- Upstream of aggregation (peak speeds calculated from filtered data)

**Changes Required**:
1. `NetworkMonitor.cs`: Add threshold check in `GetCurrentSpeed()`, store last valid reading
2. `UsageAggregator.cs`: Add threshold check in peak speed calculation (defense in depth)
3. `Settings.cs`: Add `MaxSpeedThresholdGbps` property (optional, for power users)

**Alternatives Considered**:
- Filter at display layer only - Rejected: bad data still stored, affects reports
- Filter at storage layer only - Rejected: spikes still visible momentarily
- Filter at multiple layers independently - Rejected: redundant, harder to maintain

## Existing Filter Compatibility

### Decision: Keep Existing 1GB Per-Reading Filter
**Rationale**:
- Located in `UsageRepository.CalculateUsageFromRecordsAsync()` line 247-248
- Filters total bytes per reading (not speed)
- Complementary to speed-based filtering
- No conflict or redundancy

**Code Reference**:
```csharp
if (downloadDelta > 1_000_000_000 || uploadDelta > 1_000_000_000) continue;
```

## Edge Case Handling

### Decision: Zero as Fallback When No Valid Reading Exists
**Rationale**:
- At application startup, no previous valid reading exists
- Initialize last valid reading to zero
- First valid reading will replace the zero
- Matches user expectation (no activity = zero speed)

### Decision: Filter at Query Time for Historical Data
**Rationale**:
- Existing spike data in database should not require migration
- Apply same threshold filter when querying for graphs/reports
- Simpler than database cleanup script
- Preserves raw data for potential future analysis
