namespace DataUsageReporter.Data;

/// <summary>
/// Manages data retention by cleaning up old records.
/// </summary>
public class RetentionManager
{
    private readonly IUsageRepository _repository;
    private readonly int _retentionDays;

    public RetentionManager(IUsageRepository repository, int retentionDays = 365)
    {
        _repository = repository;
        _retentionDays = retentionDays;
    }

    /// <summary>
    /// Cleans up raw records older than 1 hour.
    /// Should be called after hourly aggregation.
    /// </summary>
    public async Task CleanupRawRecordsAsync()
    {
        var cutoff = DateTime.Now.AddHours(-1);
        await _repository.DeleteRecordsBeforeAsync(cutoff);
    }

    /// <summary>
    /// Cleans up summaries older than retention period (default 1 year).
    /// Should be called daily.
    /// </summary>
    public async Task CleanupOldSummariesAsync()
    {
        var cutoff = DateTime.Now.AddDays(-_retentionDays);
        await _repository.DeleteSummariesBeforeAsync(cutoff);
    }
}
