using System.Text;
using DataUsageReporter.Core;
using DataUsageReporter.Data;

namespace DataUsageReporter.Email;

/// <summary>
/// Generates HTML and plain text usage reports.
/// </summary>
public class ReportGenerator : IReportGenerator
{
    private readonly IUsageRepository _repository;
    private readonly IUsageAggregator _usageAggregator;
    private readonly ISpeedFormatter _formatter;

    public ReportGenerator(IUsageRepository repository, IUsageAggregator usageAggregator, ISpeedFormatter formatter)
    {
        _repository = repository;
        _usageAggregator = usageAggregator;
        _formatter = formatter;
    }

    public async Task<EmailMessage> GenerateReportAsync(
        DateTime periodStart,
        DateTime periodEnd,
        ReportFrequency frequency)
    {
        var summaries = await _repository.GetSummariesAsync(periodStart, periodEnd);
        var totalUsage = await _repository.GetTotalUsageAsync(periodStart, periodEnd);

        // If no summaries exist, calculate from raw records
        if (totalUsage == null || (totalUsage.TotalDownload == 0 && totalUsage.TotalUpload == 0))
        {
            totalUsage = await _repository.CalculateUsageFromRecordsAsync(periodStart, periodEnd);
        }

        // Use centralized filtered peak speeds from UsageAggregator (same logic as graphs)
        if (totalUsage != null && summaries.Count > 0)
        {
            var filteredPeaks = await _usageAggregator.GetFilteredPeakSpeedsAsync(periodStart, periodEnd);
            totalUsage = new Data.Models.UsageSummary
            {
                PeriodStart = totalUsage.PeriodStart,
                PeriodEnd = totalUsage.PeriodEnd,
                TotalDownload = totalUsage.TotalDownload,
                TotalUpload = totalUsage.TotalUpload,
                PeakDownloadSpeed = filteredPeaks.PeakDownload,
                PeakUploadSpeed = filteredPeaks.PeakUpload,
                SampleCount = totalUsage.SampleCount
            };
        }

        // Format date range - single day vs range
        var dateRange = periodStart.Date == periodEnd.Date || periodEnd.Date == periodStart.Date.AddDays(1)
            ? $"{periodStart:d}"
            : $"{periodStart:d} - {periodEnd:d}";

        var subject = $"Network Usage Report - {GetFrequencyLabel(frequency)} ({dateRange})";
        var htmlBody = GenerateHtmlReport(summaries, totalUsage, periodStart, periodEnd, frequency);
        var plainTextBody = GeneratePlainTextReport(summaries, totalUsage, periodStart, periodEnd, frequency);

        return new EmailMessage(subject, htmlBody, plainTextBody);
    }

    private string GenerateHtmlReport(
        IReadOnlyList<Data.Models.UsageSummary> summaries,
        Data.Models.UsageSummary? totalUsage,
        DateTime periodStart,
        DateTime periodEnd,
        ReportFrequency frequency)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; }");
        sb.AppendLine("h1 { color: #333; border-bottom: 2px solid #4285f4; padding-bottom: 10px; }");
        sb.AppendLine("h2 { color: #666; }");
        sb.AppendLine(".summary { background: #f5f5f5; padding: 15px; border-radius: 8px; margin: 15px 0; }");
        sb.AppendLine(".stat { display: inline-block; width: 45%; margin: 5px 2%; }");
        sb.AppendLine(".stat-label { color: #888; font-size: 12px; }");
        sb.AppendLine(".stat-value { font-size: 24px; font-weight: bold; }");
        sb.AppendLine(".download { color: #4285f4; }");
        sb.AppendLine(".upload { color: #ea4335; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin: 15px 0; }");
        sb.AppendLine("th, td { padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }");
        sb.AppendLine("th { background: #4285f4; color: white; }");
        sb.AppendLine("tr:hover { background: #f5f5f5; }");
        sb.AppendLine(".footer { color: #999; font-size: 12px; margin-top: 20px; text-align: center; }");
        sb.AppendLine("</style></head><body>");

        // Format date range for display
        var dateDisplay = periodStart.Date == periodEnd.Date || periodEnd.Date == periodStart.Date.AddDays(1)
            ? $"{periodStart:MMMM d, yyyy}"
            : $"{periodStart:MMMM d, yyyy} - {periodEnd:MMMM d, yyyy}";

        sb.AppendLine($"<h1>Network Usage Report</h1>");
        sb.AppendLine($"<p>{GetFrequencyLabel(frequency)} Report for {dateDisplay}</p>");

        // Summary section
        sb.AppendLine("<div class='summary'>");
        sb.AppendLine("<h2>Total Usage</h2>");
        if (totalUsage != null)
        {
            sb.AppendLine($"<div class='stat'><div class='stat-label'>Download</div><div class='stat-value download'>{_formatter.FormatBytes(totalUsage.TotalDownload)}</div></div>");
            sb.AppendLine($"<div class='stat'><div class='stat-label'>Upload</div><div class='stat-value upload'>{_formatter.FormatBytes(totalUsage.TotalUpload)}</div></div>");
            sb.AppendLine($"<div class='stat'><div class='stat-label'>Peak Download</div><div class='stat-value download'>{_formatter.FormatSpeed(totalUsage.PeakDownloadSpeed)}</div></div>");
            sb.AppendLine($"<div class='stat'><div class='stat-label'>Peak Upload</div><div class='stat-value upload'>{_formatter.FormatSpeed(totalUsage.PeakUploadSpeed)}</div></div>");
        }
        else
        {
            sb.AppendLine("<p>No usage data available for this period.</p>");
        }
        sb.AppendLine("</div>");

        // Daily breakdown (only show if we have multiple days of data)
        if (summaries.Count > 0)
        {
            var dailyData = summaries
                .GroupBy(s => s.GetPeriodStart().Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Download = g.Sum(s => s.TotalDownload),
                    Upload = g.Sum(s => s.TotalUpload)
                })
                .Where(d => d.Download > 0 || d.Upload > 0) // Filter out zero days
                .OrderBy(d => d.Date)
                .ToList();

            if (dailyData.Count > 1) // Only show breakdown if multiple days
            {
                sb.AppendLine("<h2>Daily Breakdown</h2>");
                sb.AppendLine("<table><tr><th>Date</th><th>Download</th><th>Upload</th></tr>");

                foreach (var day in dailyData)
                {
                    sb.AppendLine($"<tr><td>{day.Date:MMM d}</td><td class='download'>{_formatter.FormatBytes(day.Download)}</td><td class='upload'>{_formatter.FormatBytes(day.Upload)}</td></tr>");
                }

                sb.AppendLine("</table>");
            }
        }

        sb.AppendLine("<div class='footer'>Generated by Data Usage Reporter</div>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    private string GeneratePlainTextReport(
        IReadOnlyList<Data.Models.UsageSummary> summaries,
        Data.Models.UsageSummary? totalUsage,
        DateTime periodStart,
        DateTime periodEnd,
        ReportFrequency frequency)
    {
        var sb = new StringBuilder();

        // Format date range for display
        var dateDisplay = periodStart.Date == periodEnd.Date || periodEnd.Date == periodStart.Date.AddDays(1)
            ? $"{periodStart:d}"
            : $"{periodStart:d} - {periodEnd:d}";

        sb.AppendLine("NETWORK USAGE REPORT");
        sb.AppendLine("====================");
        sb.AppendLine($"{GetFrequencyLabel(frequency)} Report for {dateDisplay}");
        sb.AppendLine();

        sb.AppendLine("TOTAL USAGE");
        sb.AppendLine("-----------");
        if (totalUsage != null)
        {
            sb.AppendLine($"Download:      {_formatter.FormatBytes(totalUsage.TotalDownload)}");
            sb.AppendLine($"Upload:        {_formatter.FormatBytes(totalUsage.TotalUpload)}");
            sb.AppendLine($"Peak Download: {_formatter.FormatSpeed(totalUsage.PeakDownloadSpeed)}");
            sb.AppendLine($"Peak Upload:   {_formatter.FormatSpeed(totalUsage.PeakUploadSpeed)}");
        }
        else
        {
            sb.AppendLine("No usage data available for this period.");
        }
        sb.AppendLine();

        if (summaries.Count > 0)
        {
            var dailyData = summaries
                .GroupBy(s => s.GetPeriodStart().Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Download = g.Sum(s => s.TotalDownload),
                    Upload = g.Sum(s => s.TotalUpload)
                })
                .Where(d => d.Download > 0 || d.Upload > 0)
                .OrderBy(d => d.Date)
                .ToList();

            if (dailyData.Count > 1)
            {
                sb.AppendLine("DAILY BREAKDOWN");
                sb.AppendLine("---------------");

                foreach (var day in dailyData)
                {
                    sb.AppendLine($"{day.Date:MMM d}:  Down: {_formatter.FormatBytes(day.Download),10}  Up: {_formatter.FormatBytes(day.Upload),10}");
                }
            }
        }

        sb.AppendLine();
        sb.AppendLine("Generated by Data Usage Reporter");

        return sb.ToString();
    }

    private static string GetFrequencyLabel(ReportFrequency frequency)
    {
        return frequency switch
        {
            ReportFrequency.Daily => "Daily",
            ReportFrequency.Weekly => "Weekly",
            ReportFrequency.Monthly => "Monthly",
            _ => "Usage"
        };
    }
}
