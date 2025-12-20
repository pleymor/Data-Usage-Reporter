using DataUsageReporter.Data.Models;

namespace DataUsageReporter.Core;

/// <summary>
/// Aggregates raw records into summaries and provides data points for graphs.
/// </summary>
public interface IUsageAggregator
{
    /// <summary>
    /// Aggregates raw records for the specified hour.
    /// </summary>
    /// <param name="hourStart">Start of the hour (truncated to hour)</param>
    /// <returns>Aggregated summary, or null if no records</returns>
    Task<UsageSummary?> AggregateHourAsync(DateTime hourStart);

    /// <summary>
    /// Aggregates summaries for display at different granularities.
    /// </summary>
    Task<IReadOnlyList<UsageDataPoint>> GetDataPointsAsync(
        DateTime from,
        DateTime to,
        TimeGranularity granularity
    );
}
