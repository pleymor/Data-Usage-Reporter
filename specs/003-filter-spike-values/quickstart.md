# Quickstart: Filter Impossible Spike Values

**Feature**: 003-filter-spike-values
**Date**: 2025-12-20

## Implementation Steps

### 1. Add Threshold to Settings

In `src/DataUsageReporter/Data/Settings.cs`, add the new property:

```csharp
public int MaxSpeedThresholdGbps { get; set; } = 10;
```

### 2. Modify NetworkMonitor

In `src/DataUsageReporter/Core/NetworkMonitor.cs`:

1. Add fields for filtering state:
```csharp
private SpeedReading _lastValidSpeed = new(0, 0, DateTime.Now);
private readonly long _maxBytesPerSecond;
private const long BytesPerGbps = 125_000_000L;
```

2. Initialize threshold from settings in constructor:
```csharp
_maxBytesPerSecond = settings.MaxSpeedThresholdGbps * BytesPerGbps;
```

3. Add spike detection method:
```csharp
private bool IsSpike(long downloadBps, long uploadBps)
{
    return downloadBps > _maxBytesPerSecond || uploadBps > _maxBytesPerSecond;
}
```

4. Modify `GetCurrentSpeed()` to filter spikes:
```csharp
// After calculating deltas, before returning:
if (IsSpike(downloadDelta, uploadDelta))
{
    return _lastValidSpeed;
}

var reading = new SpeedReading(downloadDelta, uploadDelta, timestamp);
_lastValidSpeed = reading;
return reading;
```

### 3. Add Peak Speed Filtering to UsageAggregator

In `src/DataUsageReporter/Core/UsageAggregator.cs`, modify the peak speed calculation:

```csharp
// In AggregateHourAsync, when calculating peak speeds:
var downloadSpeed = (long)(downloadDelta / timeDelta);
if (downloadSpeed <= _maxBytesPerSecond && downloadSpeed > peakDownloadSpeed)
{
    peakDownloadSpeed = downloadSpeed;
}
```

### 4. Pass Settings to NetworkMonitor

Ensure the settings object is passed to NetworkMonitor constructor so it can read `MaxSpeedThresholdGbps`.

## Testing

1. Build the application
2. Run and monitor network usage
3. Verify:
   - Speed display never shows impossibly high values
   - Graphs show realistic data ranges
   - Existing functionality unchanged

## Files Modified

- `src/DataUsageReporter/Data/Settings.cs` - Add MaxSpeedThresholdGbps property
- `src/DataUsageReporter/Core/NetworkMonitor.cs` - Add spike filtering logic
- `src/DataUsageReporter/Core/UsageAggregator.cs` - Add peak speed threshold check
