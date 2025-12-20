using DataUsageReporter.Data;
using DataUsageReporter.Data.Models;

namespace DataUsageReporter.Core;

/// <summary>
/// Aggregates raw usage records into hourly summaries and provides data points for graphs.
/// </summary>
public class UsageAggregator : IUsageAggregator
{
    private readonly IUsageRepository _repository;
    private readonly long _maxBytesPerSecond;
    private const long BytesPerGbps = 125_000_000L;

    public UsageAggregator(IUsageRepository repository, int maxSpeedThresholdGbps = 10)
    {
        _repository = repository;
        _maxBytesPerSecond = maxSpeedThresholdGbps * BytesPerGbps;
    }

    public async Task<UsageSummary?> AggregateHourAsync(DateTime hourStart)
    {
        // Truncate to hour
        hourStart = new DateTime(hourStart.Year, hourStart.Month, hourStart.Day, hourStart.Hour, 0, 0);
        var hourEnd = hourStart.AddHours(1);

        var records = await _repository.GetRecordsSinceAsync(hourStart);
        var hourRecords = records.Where(r => r.GetDateTime() < hourEnd).ToList();

        if (hourRecords.Count < 2)
            return null;

        // Calculate totals from delta between first and last record
        var firstRecord = hourRecords.First();
        var lastRecord = hourRecords.Last();

        var totalDownload = lastRecord.BytesReceived - firstRecord.BytesReceived;
        var totalUpload = lastRecord.BytesSent - firstRecord.BytesSent;

        // Handle counter resets
        if (totalDownload < 0) totalDownload = 0;
        if (totalUpload < 0) totalUpload = 0;

        // Calculate peak speeds
        long peakDownloadSpeed = 0;
        long peakUploadSpeed = 0;

        for (int i = 1; i < hourRecords.Count; i++)
        {
            var prev = hourRecords[i - 1];
            var curr = hourRecords[i];

            var timeDelta = (curr.GetDateTime() - prev.GetDateTime()).TotalSeconds;
            if (timeDelta <= 0) continue;

            var downloadDelta = curr.BytesReceived - prev.BytesReceived;
            var uploadDelta = curr.BytesSent - prev.BytesSent;

            if (downloadDelta < 0) downloadDelta = 0;
            if (uploadDelta < 0) uploadDelta = 0;

            var downloadSpeed = (long)(downloadDelta / timeDelta);
            var uploadSpeed = (long)(uploadDelta / timeDelta);

            if (downloadSpeed > peakDownloadSpeed) peakDownloadSpeed = downloadSpeed;
            if (uploadSpeed > peakUploadSpeed) peakUploadSpeed = uploadSpeed;
        }

        return new UsageSummary(
            hourStart,
            hourEnd,
            totalDownload,
            totalUpload,
            peakDownloadSpeed,
            peakUploadSpeed,
            hourRecords.Count
        );
    }

    public async Task<IReadOnlyList<UsageDataPoint>> GetDataPointsAsync(
        DateTime from,
        DateTime to,
        TimeGranularity granularity)
    {
        if (granularity == TimeGranularity.Minute)
        {
            return await GetMinuteDataPointsAsync(from, to);
        }

        var summaries = await _repository.GetSummariesAsync(from, to);

        return granularity switch
        {
            TimeGranularity.Hour => AggregateByHour(summaries),
            TimeGranularity.Day => AggregateByPeriod(summaries, s => s.GetPeriodStart().Date),
            TimeGranularity.Month => AggregateByPeriod(summaries, s => new DateTime(s.GetPeriodStart().Year, s.GetPeriodStart().Month, 1)),
            TimeGranularity.Year => AggregateByPeriod(summaries, s => new DateTime(s.GetPeriodStart().Year, 1, 1)),
            _ => throw new ArgumentOutOfRangeException(nameof(granularity))
        };
    }

    private async Task<IReadOnlyList<UsageDataPoint>> GetMinuteDataPointsAsync(DateTime from, DateTime to)
    {
        var records = await _repository.GetRecordsSinceAsync(from);
        var filteredRecords = records.Where(r => r.GetDateTime() <= to).ToList();

        if (filteredRecords.Count < 2)
            return Array.Empty<UsageDataPoint>();

        var dataPoints = new List<UsageDataPoint>();

        for (int i = 1; i < filteredRecords.Count; i++)
        {
            var prev = filteredRecords[i - 1];
            var curr = filteredRecords[i];

            var downloadDelta = curr.BytesReceived - prev.BytesReceived;
            var uploadDelta = curr.BytesSent - prev.BytesSent;

            if (downloadDelta < 0) downloadDelta = 0;
            if (uploadDelta < 0) uploadDelta = 0;

            // Skip data points with large time gaps (app was closed/restarted)
            // Normal interval is ~1 second, so anything > 10 seconds indicates a gap
            var timeDelta = (curr.GetDateTime() - prev.GetDateTime()).TotalSeconds;
            if (timeDelta > 10)
            {
                // Skip this data point - it spans a gap when app was closed
                continue;
            }

            dataPoints.Add(new UsageDataPoint(curr.GetDateTime(), downloadDelta, uploadDelta));
        }

        return dataPoints;
    }

    private IReadOnlyList<UsageDataPoint> AggregateByHour(IReadOnlyList<UsageSummary> summaries)
    {
        // Max bytes per hour = max bytes/sec * 3600 seconds
        var maxBytesPerHour = _maxBytesPerSecond * 3600;

        return summaries
            .Select(s => new UsageDataPoint(
                s.GetPeriodStart(),
                Math.Min(s.TotalDownload, maxBytesPerHour),
                Math.Min(s.TotalUpload, maxBytesPerHour)))
            .ToList();
    }

    private IReadOnlyList<UsageDataPoint> AggregateByPeriod(
        IReadOnlyList<UsageSummary> summaries,
        Func<UsageSummary, DateTime> keySelector)
    {
        // Max bytes per hour = max bytes/sec * 3600 seconds
        var maxBytesPerHour = _maxBytesPerSecond * 3600;

        return summaries
            .GroupBy(keySelector)
            .Select(g => new UsageDataPoint(
                g.Key,
                // Cap each hourly summary before summing to filter spike contributions
                g.Sum(s => Math.Min(s.TotalDownload, maxBytesPerHour)),
                g.Sum(s => Math.Min(s.TotalUpload, maxBytesPerHour))))
            .OrderBy(d => d.Timestamp)
            .ToList();
    }

    public async Task<(long PeakDownload, long PeakUpload)> GetFilteredPeakSpeedsAsync(DateTime from, DateTime to)
    {
        // Calculate peak from minute-level data points (already filtered for time gaps)
        var dataPoints = await GetMinuteDataPointsAsync(from, to);

        if (dataPoints.Count == 0)
            return (0, 0);

        // Find max bytes transferred in any single interval
        // Since intervals are ~1 second, this approximates peak speed
        long peakDownload = dataPoints.Max(d => d.DownloadBytes);
        long peakUpload = dataPoints.Max(d => d.UploadBytes);

        // Cap at max threshold
        peakDownload = Math.Min(peakDownload, _maxBytesPerSecond);
        peakUpload = Math.Min(peakUpload, _maxBytesPerSecond);

        return (peakDownload, peakUpload);
    }
}
