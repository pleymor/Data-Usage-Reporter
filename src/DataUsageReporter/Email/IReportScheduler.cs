namespace DataUsageReporter.Email;

/// <summary>
/// Manages scheduled report execution.
/// </summary>
public interface IReportScheduler
{
    /// <summary>
    /// Starts the scheduler background service.
    /// </summary>
    void Start();

    /// <summary>
    /// Stops the scheduler.
    /// </summary>
    void Stop();

    /// <summary>
    /// Gets the next scheduled run time.
    /// </summary>
    DateTime? GetNextRunTime();

    /// <summary>
    /// Recalculates the next run time after schedule changes.
    /// </summary>
    void RecalculateNextRun();

    /// <summary>
    /// Event raised when a report is sent or fails.
    /// </summary>
    event EventHandler<ReportEventArgs>? ReportCompleted;
}

/// <summary>
/// Event arguments for report completion.
/// </summary>
public record ReportEventArgs(
    bool Success,
    DateTime Timestamp,
    string? ErrorMessage = null
);
