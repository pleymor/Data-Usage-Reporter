# Research: Graph Shows All Historical Data (Bug Fix)

**Feature**: 008-extended-graph-history
**Date**: 2025-12-21

## Research Task: Why does the graph only show today's data?

### Investigation

Traced the data flow from GraphPanel to the database:

1. **GraphPanel.cs:60-67** - `RefreshDataAsync()`:
   ```csharp
   var from = DateTime.Now.AddYears(-5);
   var to = DateTime.Now;
   var dataPoints = await _aggregator.GetDataPointsAsync(from, to, TimeGranularity.Minute);
   ```
   - Requests 5 years of data with Minute granularity

2. **UsageAggregator.cs:85-88** - `GetDataPointsAsync()`:
   ```csharp
   if (granularity == TimeGranularity.Minute)
   {
       return await GetMinuteDataPointsAsync(from, to);
   }
   ```
   - Minute granularity routes to `GetMinuteDataPointsAsync`

3. **UsageAggregator.cs:102-104** - `GetMinuteDataPointsAsync()`:
   ```csharp
   var records = await _repository.GetRecordsSinceAsync(from);
   var filteredRecords = records.Where(r => r.GetDateTime() <= to).ToList();
   ```
   - Queries `usage_records` table (raw per-second samples)

4. **UsageRepository.cs:43-48** - `GetRecordsSinceAsync()`:
   ```sql
   SELECT id, timestamp, bytes_received, bytes_sent
   FROM usage_records
   WHERE timestamp >= @since
   ```
   - Raw records are temporary; deleted after hourly aggregation

### Root Cause

The graph uses `TimeGranularity.Minute` which queries raw `usage_records`. Raw records are:
- Collected every ~1 second
- Aggregated into hourly summaries (`usage_summaries`)
- Deleted after aggregation

Only today's un-aggregated raw records exist. Historical data is stored only in `usage_summaries`.

### Decision: Use Hour Granularity

**Choice**: Change from `TimeGranularity.Minute` to `TimeGranularity.Hour`

**Rationale**:
- Hour granularity queries `usage_summaries` which contains all historical data
- Hourly data provides sufficient detail for trend visualization
- ~43,800 data points max (5 years) vs potentially millions of raw records
- Existing `AggregateByHour()` method already works correctly

**Alternatives Considered**:
1. **Keep raw records longer** - Rejected: Would require massive storage, violates Resource Efficiency principle
2. **Hybrid approach (recent minute + historical hour)** - Rejected: Adds complexity for minimal benefit
3. **Day granularity** - Rejected: Too coarse for recent data visualization

### Verification

The existing `GetSummariesAsync()` correctly retrieves historical data:
```csharp
// UsageRepository.cs:103-118
command.CommandText = @"
    SELECT id, period_start, period_end, total_download, total_upload,
           peak_download_speed, peak_upload_speed, sample_count
    FROM usage_summaries
    WHERE period_start >= @from AND period_start < @to
    ORDER BY period_start ASC";
```

No additional code changes needed beyond the granularity parameter.
