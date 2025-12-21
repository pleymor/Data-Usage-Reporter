using System.Text;
using DataUsageReporter.Core;
using DataUsageReporter.Core.Localization;
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
    private readonly ILocalizationService _localization;
    private readonly IEmailReportGraphRenderer? _graphRenderer;

    public ReportGenerator(
        IUsageRepository repository,
        IUsageAggregator usageAggregator,
        ISpeedFormatter formatter,
        ILocalizationService localization,
        IEmailReportGraphRenderer? graphRenderer = null)
    {
        _repository = repository;
        _usageAggregator = usageAggregator;
        _formatter = formatter;
        _localization = localization;
        _graphRenderer = graphRenderer;
    }

    public async Task<EmailMessage> GenerateReportAsync(
        DateTime periodStart,
        DateTime periodEnd,
        ReportFrequency frequency,
        string? customSubject = null)
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

        // Render graph based on frequency
        InlineAttachment? graphAttachment = null;
        if (_graphRenderer != null)
        {
            graphAttachment = await RenderGraphForFrequencyAsync(frequency, periodStart, periodEnd);
        }

        // Build table rows for the report (always daily granularity)
        // For Daily: last 7 days, for Weekly/Monthly: last 31 days
        var tablePeriodStart = frequency == ReportFrequency.Daily
            ? periodEnd.Date.AddDays(-6)
            : periodEnd.Date.AddDays(-30);
        var tableSummaries = await _repository.GetSummariesAsync(tablePeriodStart, periodEnd);
        var tableRows = BuildTableRows(tableSummaries, TimeGranularity.Day).ToList();

        // Add current day's partial data if not already in table
        var today = DateTime.Now.Date;
        if (!tableRows.Any(r => r.Date.Date == today))
        {
            var todayUsage = await _repository.CalculateUsageFromRecordsAsync(today, DateTime.Now);
            if (todayUsage != null && (todayUsage.TotalDownload > 0 || todayUsage.TotalUpload > 0))
            {
                tableRows.Add(new ReportTableRow(
                    Date: today,
                    DownloadMB: todayUsage.TotalDownload / 1_048_576.0,
                    UploadMB: todayUsage.TotalUpload / 1_048_576.0,
                    PeakDownloadMbps: todayUsage.PeakDownloadSpeed / 1_000_000.0,
                    PeakUploadMbps: todayUsage.PeakUploadSpeed / 1_000_000.0
                ));
                tableRows = tableRows.OrderBy(r => r.Date).ToList();
            }
        }

        var subject = !string.IsNullOrWhiteSpace(customSubject)
            ? customSubject
            : _localization.GetString("Email_Subject", DateTime.Now);
        var htmlBody = GenerateHtmlReport(summaries, totalUsage, periodStart, periodEnd, frequency, graphAttachment?.ContentId, tableRows);
        var plainTextBody = GeneratePlainTextReport(summaries, totalUsage, periodStart, periodEnd, frequency, tableRows);

        var attachments = graphAttachment != null
            ? new List<InlineAttachment> { graphAttachment }
            : null;

        return new EmailMessage(subject, htmlBody, plainTextBody, attachments);
    }

    private async Task<InlineAttachment?> RenderGraphForFrequencyAsync(
        ReportFrequency frequency,
        DateTime periodStart,
        DateTime periodEnd)
    {
        if (_graphRenderer == null) return null;

        try
        {
            byte[] graphBytes;
            var contentId = $"usage-graph-{Guid.NewGuid():N}";

            switch (frequency)
            {
                case ReportFrequency.Daily:
                    // Get hourly data for the report day (from summaries)
                    var hourlyData = (await _usageAggregator.GetDataPointsAsync(periodStart, periodEnd, TimeGranularity.Hour)).ToList();

                    // Add today's hourly data from raw records for hours not yet aggregated
                    var todayStart = DateTime.Now.Date;
                    var rawRecords = await _repository.GetRecordsSinceAsync(todayStart);
                    if (rawRecords.Count >= 2)
                    {
                        var hourlyFromRaw = new Dictionary<int, (long Download, long Upload)>();
                        for (int i = 1; i < rawRecords.Count; i++)
                        {
                            var prev = rawRecords[i - 1];
                            var curr = rawRecords[i];
                            var hour = curr.GetDateTime().Hour;

                            var downloadDelta = Math.Max(0, curr.BytesReceived - prev.BytesReceived);
                            var uploadDelta = Math.Max(0, curr.BytesSent - prev.BytesSent);

                            // Skip large gaps
                            if ((curr.GetDateTime() - prev.GetDateTime()).TotalSeconds > 10)
                                continue;

                            if (!hourlyFromRaw.ContainsKey(hour))
                                hourlyFromRaw[hour] = (0, 0);

                            var existing = hourlyFromRaw[hour];
                            hourlyFromRaw[hour] = (existing.Download + downloadDelta, existing.Upload + uploadDelta);
                        }

                        // Add hours from raw data that aren't in summaries
                        foreach (var kvp in hourlyFromRaw)
                        {
                            var hourTimestamp = todayStart.AddHours(kvp.Key);
                            if (!hourlyData.Any(h => h.Timestamp.Hour == kvp.Key && h.Timestamp.Date == todayStart))
                            {
                                hourlyData.Add(new UsageDataPoint(hourTimestamp, kvp.Value.Download, kvp.Value.Upload));
                            }
                        }
                    }

                    graphBytes = _graphRenderer.RenderHourlyGraph(hourlyData, periodStart);
                    break;

                case ReportFrequency.Weekly:
                    // Get daily data for the last 7 days
                    var dailyData = await _usageAggregator.GetDataPointsAsync(periodStart, periodEnd, TimeGranularity.Day);
                    graphBytes = _graphRenderer.RenderDailyGraph(dailyData, periodEnd);
                    break;

                case ReportFrequency.Monthly:
                    // Get daily data for weekly aggregation (5 weeks)
                    var weeklyData = await _usageAggregator.GetDataPointsAsync(periodStart, periodEnd, TimeGranularity.Day);
                    graphBytes = _graphRenderer.RenderWeeklyGraph(weeklyData, periodEnd);
                    break;

                default:
                    return null;
            }

            return new InlineAttachment(
                contentId,
                graphBytes,
                "image/png",
                "usage-graph.png"
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to render graph: {ex.Message}");
            return null;
        }
    }

    private string GenerateHtmlReport(
        IReadOnlyList<Data.Models.UsageSummary> summaries,
        Data.Models.UsageSummary? totalUsage,
        DateTime periodStart,
        DateTime periodEnd,
        ReportFrequency frequency,
        string? graphContentId = null,
        IReadOnlyList<ReportTableRow>? tableRows = null)
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

        // Format date range for display (culture-aware)
        var dateDisplay = periodStart.Date == periodEnd.Date || periodEnd.Date == periodStart.Date.AddDays(1)
            ? _localization.FormatDate(periodStart, "d MMMM yyyy")
            : $"{_localization.FormatDate(periodStart, "d MMMM yyyy")} - {_localization.FormatDate(periodEnd, "d MMMM yyyy")}";

        sb.AppendLine($"<h1>{_localization.GetString("Email_ReportTitle", dateDisplay)}</h1>");

        // Usage graph (embedded image via CID)
        if (!string.IsNullOrEmpty(graphContentId))
        {
            var graphTitle = frequency switch
            {
                ReportFrequency.Daily => _localization.GetString("Email_GraphHourly"),
                ReportFrequency.Weekly => _localization.GetString("Email_GraphDaily"),
                ReportFrequency.Monthly => _localization.GetString("Email_GraphWeekly"),
                _ => _localization.GetString("Graph_Title")
            };

            sb.AppendLine($"<h2>{graphTitle}</h2>");
            sb.AppendLine($"<img src=\"cid:{graphContentId}\" alt=\"{graphTitle}\" style=\"max-width: 100%; height: auto;\" />");
        }

        // Usage table (always daily granularity - 7 days for daily, 31 days for weekly/monthly)
        if (tableRows != null && tableRows.Count > 0)
        {
            sb.AppendLine(GenerateTableHtml(tableRows, TimeGranularity.Day));
        }

        sb.AppendLine($"<div class='footer'>{_localization.GetString("Email_Footer")}</div>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    private string GeneratePlainTextReport(
        IReadOnlyList<Data.Models.UsageSummary> summaries,
        Data.Models.UsageSummary? totalUsage,
        DateTime periodStart,
        DateTime periodEnd,
        ReportFrequency frequency,
        IReadOnlyList<ReportTableRow>? tableRows = null)
    {
        var sb = new StringBuilder();

        // Format date range for display (culture-aware)
        var dateDisplay = periodStart.Date == periodEnd.Date || periodEnd.Date == periodStart.Date.AddDays(1)
            ? _localization.FormatDate(periodStart, "d MMMM yyyy")
            : $"{_localization.FormatDate(periodStart, "d MMMM yyyy")} - {_localization.FormatDate(periodEnd, "d MMMM yyyy")}";

        sb.AppendLine(_localization.GetString("Email_ReportTitle", dateDisplay).ToUpperInvariant());
        sb.AppendLine("========================================");
        sb.AppendLine();

        // Usage table (always daily granularity)
        if (tableRows != null && tableRows.Count > 0)
        {
            sb.AppendLine(GenerateTablePlainText(tableRows, TimeGranularity.Day));
        }

        sb.AppendLine();
        sb.AppendLine(_localization.GetString("Email_Footer"));

        return sb.ToString();
    }

    /// <summary>
    /// Builds table rows from usage summaries grouped by the specified granularity.
    /// </summary>
    internal IReadOnlyList<ReportTableRow> BuildTableRows(
        IReadOnlyList<Data.Models.UsageSummary> summaries,
        TimeGranularity granularity)
    {
        if (summaries.Count == 0)
            return Array.Empty<ReportTableRow>();

        var grouped = granularity switch
        {
            TimeGranularity.Hour => summaries.GroupBy(s => new DateTime(
                s.GetPeriodStart().Year,
                s.GetPeriodStart().Month,
                s.GetPeriodStart().Day,
                s.GetPeriodStart().Hour, 0, 0)),
            TimeGranularity.Day => summaries.GroupBy(s => s.GetPeriodStart().Date),
            TimeGranularity.Week => summaries.GroupBy(s =>
                GetWeekStart(s.GetPeriodStart())),
            _ => summaries.GroupBy(s => s.GetPeriodStart().Date)
        };

        return grouped
            .Select(g => new ReportTableRow(
                Date: g.Key,
                DownloadMB: g.Sum(s => s.TotalDownload) / 1_048_576.0,
                UploadMB: g.Sum(s => s.TotalUpload) / 1_048_576.0,
                PeakDownloadMbps: g.Max(s => s.PeakDownloadSpeed) / 1_000_000.0,
                PeakUploadMbps: g.Max(s => s.PeakUploadSpeed) / 1_000_000.0))
            .OrderBy(r => r.Date)
            .ToList();
    }

    /// <summary>
    /// Gets the start of the ISO week (Monday) for a given date.
    /// </summary>
    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-diff);
    }

    /// <summary>
    /// Generates an HTML table from report rows.
    /// </summary>
    internal string GenerateTableHtml(IReadOnlyList<ReportTableRow> rows, TimeGranularity granularity)
    {
        if (rows.Count == 0)
            return $"<p>{_localization.GetString("Graph_NoData")}</p>";

        var sb = new StringBuilder();
        sb.AppendLine("<table>");
        sb.AppendLine("<tr>");
        sb.AppendLine($"<th>{_localization.GetString("Email_TableHeader_Date")}</th>");
        sb.AppendLine($"<th>{_localization.GetString("Email_TableHeader_DownloadMB")}</th>");
        sb.AppendLine($"<th>{_localization.GetString("Email_TableHeader_UploadMB")}</th>");
        sb.AppendLine($"<th>{_localization.GetString("Email_TableHeader_PeakDownload")}</th>");
        sb.AppendLine($"<th>{_localization.GetString("Email_TableHeader_PeakUpload")}</th>");
        sb.AppendLine("</tr>");

        foreach (var row in rows)
        {
            var dateFormat = granularity == TimeGranularity.Hour ? "HH:mm" :
                             granularity == TimeGranularity.Week ? "'W'ww" : "MM/dd";
            var dateDisplay = granularity == TimeGranularity.Week
                ? _localization.GetString("Email_Week", System.Globalization.ISOWeek.GetWeekOfYear(row.Date))
                : row.Date.ToString(dateFormat);

            sb.AppendLine("<tr>");
            sb.AppendLine($"<td>{dateDisplay}</td>");
            sb.AppendLine($"<td class='download'>{row.DownloadMB:F1}</td>");
            sb.AppendLine($"<td class='upload'>{row.UploadMB:F1}</td>");
            sb.AppendLine($"<td class='download'>{row.PeakDownloadMbps:F2}</td>");
            sb.AppendLine($"<td class='upload'>{row.PeakUploadMbps:F2}</td>");
            sb.AppendLine("</tr>");
        }

        // Add totals row
        var totalDownload = rows.Sum(r => r.DownloadMB);
        var totalUpload = rows.Sum(r => r.UploadMB);
        var peakDownload = rows.Max(r => r.PeakDownloadMbps);
        var peakUpload = rows.Max(r => r.PeakUploadMbps);

        sb.AppendLine("<tr style='font-weight: bold; background: #e8e8e8;'>");
        sb.AppendLine($"<td>{_localization.GetString("Email_TableTotal")}</td>");
        sb.AppendLine($"<td class='download'>{totalDownload:F1}</td>");
        sb.AppendLine($"<td class='upload'>{totalUpload:F1}</td>");
        sb.AppendLine($"<td class='download'>{peakDownload:F2}</td>");
        sb.AppendLine($"<td class='upload'>{peakUpload:F2}</td>");
        sb.AppendLine("</tr>");

        sb.AppendLine("</table>");
        return sb.ToString();
    }

    /// <summary>
    /// Generates a plain text table from report rows.
    /// </summary>
    internal string GenerateTablePlainText(IReadOnlyList<ReportTableRow> rows, TimeGranularity granularity)
    {
        if (rows.Count == 0)
            return _localization.GetString("Graph_NoData");

        var sb = new StringBuilder();

        // Header
        sb.AppendLine($"{"Date",-12} {"DL (MB)",10} {"UL (MB)",10} {"Peak DL",10} {"Peak UL",10}");
        sb.AppendLine(new string('-', 54));

        foreach (var row in rows)
        {
            var dateFormat = granularity == TimeGranularity.Hour ? "HH:mm" :
                             granularity == TimeGranularity.Week ? "'W'ww" : "MM/dd";
            var dateDisplay = granularity == TimeGranularity.Week
                ? $"Week {System.Globalization.ISOWeek.GetWeekOfYear(row.Date)}"
                : row.Date.ToString(dateFormat);

            sb.AppendLine($"{dateDisplay,-12} {row.DownloadMB,10:F1} {row.UploadMB,10:F1} {row.PeakDownloadMbps,10:F2} {row.PeakUploadMbps,10:F2}");
        }

        // Totals
        var totalDownload = rows.Sum(r => r.DownloadMB);
        var totalUpload = rows.Sum(r => r.UploadMB);
        var peakDownload = rows.Max(r => r.PeakDownloadMbps);
        var peakUpload = rows.Max(r => r.PeakUploadMbps);

        sb.AppendLine(new string('-', 54));
        sb.AppendLine($"{"Total",-12} {totalDownload,10:F1} {totalUpload,10:F1} {peakDownload,10:F2} {peakUpload,10:F2}");

        return sb.ToString();
    }
}
