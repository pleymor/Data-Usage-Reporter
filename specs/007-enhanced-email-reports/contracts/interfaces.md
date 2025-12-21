# Interface Contracts: Enhanced Email Reports

**Feature**: 007-enhanced-email-reports
**Date**: 2025-12-21

## New Interfaces

### IEmailReportGraphRenderer

Renders usage graphs as PNG images for email embedding.

```csharp
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
```

## Modified Interfaces

### IUsageAggregator (Extended)

Add Week granularity support to existing interface.

```csharp
namespace DataUsageReporter.Core;

public interface IUsageAggregator
{
    // Existing methods unchanged...

    /// <summary>
    /// Gets weekly aggregated data points for monthly reports.
    /// </summary>
    /// <param name="from">Start date</param>
    /// <param name="to">End date</param>
    /// <returns>Weekly aggregated data points</returns>
    Task<IReadOnlyList<UsageDataPoint>> GetWeeklyDataPointsAsync(DateTime from, DateTime to);
}
```

### IReportGenerator (Extended)

The existing interface signature remains unchanged, but the implementation will:
- Accept `ReportFrequency` to determine graph/table format
- Return `EmailMessage` with embedded graph

```csharp
namespace DataUsageReporter.Email;

public interface IReportGenerator
{
    // Signature unchanged - frequency parameter already exists
    Task<EmailMessage> GenerateReportAsync(
        DateTime periodStart,
        DateTime periodEnd,
        ReportFrequency frequency,
        string? customSubject = null
    );
}
```

### EmailMessage (Extended)

Add support for inline attachments.

```csharp
namespace DataUsageReporter.Email;

/// <summary>
/// Email message with optional inline attachments.
/// </summary>
public record EmailMessage(
    string Subject,
    string HtmlBody,
    string PlainTextBody,
    IReadOnlyList<InlineAttachment>? InlineAttachments = null
);

/// <summary>
/// Inline attachment for email embedding.
/// </summary>
public record InlineAttachment(
    string ContentId,
    byte[] Data,
    string MimeType,
    string FileName
);
```

## Data Transfer Objects

### ReportTableRow

```csharp
namespace DataUsageReporter.Email;

/// <summary>
/// A single row in the email report table.
/// </summary>
public record ReportTableRow(
    DateTime Date,
    double DownloadMB,
    double UploadMB,
    double PeakDownloadMbps,
    double PeakUploadMbps
);
```

## Localization Keys (New)

Add to `Strings.resx` and `Strings.fr.resx`:

| Key | English | French |
|-----|---------|--------|
| `Email_TableHeader_Date` | Date | Date |
| `Email_TableHeader_DownloadMB` | Download (MB) | Téléchargement (Mo) |
| `Email_TableHeader_UploadMB` | Upload (MB) | Téléversement (Mo) |
| `Email_TableHeader_PeakDownload` | Peak ↓ (Mbps) | Pointe ↓ (Mbps) |
| `Email_TableHeader_PeakUpload` | Peak ↑ (Mbps) | Pointe ↑ (Mbps) |
| `Email_TableTotal` | Total | Total |
| `Email_GraphHourly` | Hourly Usage | Utilisation horaire |
| `Email_GraphDaily` | Daily Usage | Utilisation quotidienne |
| `Email_GraphWeekly` | Weekly Usage | Utilisation hebdomadaire |
| `Email_Week` | Week {0} | Semaine {0} |
