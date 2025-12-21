using DataUsageReporter.Core;

namespace DataUsageReporter.Email;

/// <summary>
/// Renders usage graphs as PNG images for email reports.
/// </summary>
public interface IEmailReportGraphRenderer
{
    /// <summary>
    /// Renders an hourly graph for daily reports (24 data points).
    /// </summary>
    /// <param name="dataPoints">Hourly usage data points</param>
    /// <param name="date">The date being reported</param>
    /// <returns>PNG image bytes</returns>
    byte[] RenderHourlyGraph(IReadOnlyList<UsageDataPoint> dataPoints, DateTime date);

    /// <summary>
    /// Renders a daily graph for weekly reports (7 data points).
    /// </summary>
    /// <param name="dataPoints">Daily usage data points</param>
    /// <param name="periodEnd">End date of the period</param>
    /// <returns>PNG image bytes</returns>
    byte[] RenderDailyGraph(IReadOnlyList<UsageDataPoint> dataPoints, DateTime periodEnd);

    /// <summary>
    /// Renders a weekly graph for monthly reports (5 data points).
    /// </summary>
    /// <param name="dataPoints">Weekly usage data points</param>
    /// <param name="periodEnd">End date of the period</param>
    /// <returns>PNG image bytes</returns>
    byte[] RenderWeeklyGraph(IReadOnlyList<UsageDataPoint> dataPoints, DateTime periodEnd);
}
