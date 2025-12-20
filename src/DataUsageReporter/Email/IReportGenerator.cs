using DataUsageReporter.Data;

namespace DataUsageReporter.Email;

/// <summary>
/// Generates usage report content.
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// Generates a usage report for the specified period.
    /// </summary>
    Task<EmailMessage> GenerateReportAsync(
        DateTime periodStart,
        DateTime periodEnd,
        ReportFrequency frequency
    );
}
