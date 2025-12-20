# Contracts: Filter Impossible Spike Values

**Feature**: 003-filter-spike-values
**Date**: 2025-12-20

## Overview

This feature does not introduce new public interfaces or APIs. It modifies internal behavior of existing classes. This document describes the behavioral contracts that must be maintained.

## NetworkMonitor Contract

### GetCurrentSpeed() Behavior

**Pre-condition**: At least 2 samples collected (existing behavior)

**Post-condition (NEW)**:
- Returned `SpeedReading.DownloadBytesPerSecond` <= `MaxBytesPerSecond`
- Returned `SpeedReading.UploadBytesPerSecond` <= `MaxBytesPerSecond`
- If raw reading exceeds threshold, returns previous valid reading

**Invariants**:
- `_lastValidSpeed` is always a valid SpeedReading (never null after first valid reading)
- `_maxBytesPerSecond` is derived from settings at construction time

### Internal Method Signature (Conceptual)

```csharp
// Existing method with modified behavior
public SpeedReading GetCurrentSpeed()
{
    // ... existing delta calculation ...

    // NEW: Apply threshold filtering
    if (IsSpike(downloadDelta, uploadDelta))
    {
        return _lastValidSpeed; // Hold last good value
    }

    var reading = new SpeedReading(downloadDelta, uploadDelta, timestamp);
    _lastValidSpeed = reading;
    return reading;
}

private bool IsSpike(long downloadBytesPerSec, long uploadBytesPerSec)
{
    return downloadBytesPerSec > _maxBytesPerSecond
        || uploadBytesPerSec > _maxBytesPerSecond;
}
```

## UsageAggregator Contract

### AggregateHourAsync() Behavior

**Post-condition (NEW)**:
- `PeakDownloadSpeed` <= `MaxBytesPerSecond`
- `PeakUploadSpeed` <= `MaxBytesPerSecond`
- Spike readings excluded from peak calculations

## Settings Contract

### MaxSpeedThresholdGbps Property

| Property | Type | Default | Min | Max |
|----------|------|---------|-----|-----|
| MaxSpeedThresholdGbps | int | 10 | 1 | 100 |

**Behavior**:
- Persisted to settings file
- Applied to NetworkMonitor on next startup
- Changes require application restart to take effect

## Threshold Constant

```csharp
// Conversion factor: 1 Gbps = 125,000,000 bytes/second
// (1 billion bits / 8 bits per byte)
private const long BytesPerGbps = 125_000_000L;

// Calculate threshold from setting
_maxBytesPerSecond = settings.MaxSpeedThresholdGbps * BytesPerGbps;

// Default: 10 Gbps = 1,250,000,000 bytes/second
```

## Backward Compatibility

- No breaking changes to public interfaces
- Existing callers of `GetCurrentSpeed()` receive filtered data transparently
- Existing database schema unchanged
- Existing settings file extended with new property (defaults apply if missing)
