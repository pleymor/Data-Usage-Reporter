using DataUsageReporter.Data.Models;

namespace DataUsageReporter.Data;

/// <summary>
/// Data access for usage records and summaries.
/// </summary>
public interface IUsageRepository
{
    // Raw records (temporary storage)
    Task SaveRecordAsync(UsageRecord record);
    Task<IReadOnlyList<UsageRecord>> GetRecordsSinceAsync(DateTime since);
    Task DeleteRecordsBeforeAsync(DateTime before);

    // Aggregated summaries (long-term storage)
    Task SaveSummaryAsync(UsageSummary summary);
    Task<IReadOnlyList<UsageSummary>> GetSummariesAsync(DateTime from, DateTime to);
    Task DeleteSummariesBeforeAsync(DateTime before);

    // Statistics
    Task<UsageSummary?> GetTotalUsageAsync(DateTime from, DateTime to);

    // Calculate usage from raw records (for current hour before aggregation)
    Task<UsageSummary?> CalculateUsageFromRecordsAsync(DateTime from, DateTime to);
}
