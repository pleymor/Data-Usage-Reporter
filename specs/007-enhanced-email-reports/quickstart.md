# Quickstart: Enhanced Email Reports

**Feature**: 007-enhanced-email-reports
**Date**: 2025-12-21

## Prerequisites

- .NET 8.0 SDK
- Existing DataUsageReporter project built and running
- SMTP credentials configured in the application

## Implementation Order

### Phase 1: Core Infrastructure

1. **Extend TimeGranularity enum** (`Core/AggregatorTypes.cs`)
   - Add `Week` value to existing enum
   - No breaking changes to existing code

2. **Add Week aggregation** (`Core/UsageAggregator.cs`)
   - Implement `GetWeeklyDataPointsAsync` method
   - Group summaries by ISO week (Monday start)
   - Sum bytes, take max peak speeds per week

3. **Create EmailReportGraphRenderer** (`Email/EmailReportGraphRenderer.cs`)
   - New class implementing `IEmailReportGraphRenderer`
   - Use ScottPlot.Plot (headless, no WinForms dependency)
   - Render to 600x300 PNG
   - Match existing colors: #4285f4 (download), #ea4335 (upload)

### Phase 2: Email Integration

4. **Extend EmailMessage record** (`Email/IReportGenerator.cs`)
   - Add `InlineAttachments` property
   - Add `InlineAttachment` record for attachments

5. **Update EmailSender** (`Email/EmailSender.cs`)
   - Use MailKit `BodyBuilder.LinkedResources` for inline images
   - Set Content-ID from `InlineAttachment.ContentId`
   - Build MIME multipart/related message

### Phase 3: Report Generation

6. **Update ReportGenerator** (`Email/ReportGenerator.cs`)
   - Inject `IEmailReportGraphRenderer`
   - Switch on `ReportFrequency` for graph type:
     - `Daily` → hourly graph + 7-day table
     - `Weekly` → daily graph + 31-day table
     - `Monthly` → weekly graph + 31-day table
   - Build HTML table with proper columns
   - Include graph via `<img src="cid:...">` reference
   - Update plain text fallback with ASCII table

7. **Add localization strings** (`Resources/Strings.resx`, `Strings.fr.resx`)
   - Table header translations
   - Graph title translations

### Phase 4: Testing

8. **Add unit tests**
   - `EmailReportGraphRendererTests.cs` - verify PNG output
   - `ReportGeneratorTests.cs` - verify HTML/plaintext structure

## Key Code Snippets

### Headless ScottPlot Rendering

```csharp
public byte[] RenderHourlyGraph(IReadOnlyList<UsageDataPoint> dataPoints, DateTime date)
{
    var plot = new ScottPlot.Plot();

    double[] hours = Enumerable.Range(0, 24).Select(h => (double)h).ToArray();
    double[] downloads = dataPoints.Select(d => d.DownloadBytes / 1_048_576.0).ToArray();
    double[] uploads = dataPoints.Select(d => d.UploadBytes / 1_048_576.0).ToArray();

    var dlBar = plot.Add.Bars(hours, downloads);
    dlBar.Color = ScottPlot.Color.FromHex("#4285f4");
    dlBar.LegendText = "Download";

    var ulBar = plot.Add.Bars(hours.Select(h => h + 0.4).ToArray(), uploads);
    ulBar.Color = ScottPlot.Color.FromHex("#ea4335");
    ulBar.LegendText = "Upload";

    plot.Title($"Hourly Usage - {date:d}");
    plot.YLabel("MB");
    plot.XLabel("Hour");
    plot.ShowLegend();

    return plot.GetImageBytes(600, 300, ImageFormat.Png);
}
```

### MailKit Inline Image

```csharp
public async Task<bool> SendAsync(EmailMessage message)
{
    var builder = new BodyBuilder();

    // Add inline attachments
    if (message.InlineAttachments != null)
    {
        foreach (var attachment in message.InlineAttachments)
        {
            var resource = builder.LinkedResources.Add(
                attachment.FileName,
                attachment.Data,
                ContentType.Parse(attachment.MimeType));
            resource.ContentId = attachment.ContentId;
        }
    }

    builder.HtmlBody = message.HtmlBody;
    builder.TextBody = message.PlainTextBody;

    var mimeMessage = new MimeMessage();
    mimeMessage.Body = builder.ToMessageBody();
    // ... send via SmtpClient
}
```

### HTML Table Generation

```csharp
private string GenerateTableHtml(List<ReportTableRow> rows, ReportTableRow total)
{
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
        sb.AppendLine("<tr>");
        sb.AppendLine($"<td>{row.Date:MMM d}</td>");
        sb.AppendLine($"<td class='download'>{row.DownloadMB:F1}</td>");
        sb.AppendLine($"<td class='upload'>{row.UploadMB:F1}</td>");
        sb.AppendLine($"<td class='download'>{row.PeakDownloadMbps:F1}</td>");
        sb.AppendLine($"<td class='upload'>{row.PeakUploadMbps:F1}</td>");
        sb.AppendLine("</tr>");
    }

    // Total row
    sb.AppendLine("<tr style='font-weight:bold'>");
    sb.AppendLine($"<td>{_localization.GetString("Email_TableTotal")}</td>");
    sb.AppendLine($"<td class='download'>{total.DownloadMB:F1}</td>");
    sb.AppendLine($"<td class='upload'>{total.UploadMB:F1}</td>");
    sb.AppendLine($"<td class='download'>{total.PeakDownloadMbps:F1}</td>");
    sb.AppendLine($"<td class='upload'>{total.PeakUploadMbps:F1}</td>");
    sb.AppendLine("</tr>");

    sb.AppendLine("</table>");
    return sb.ToString();
}
```

## Testing Checklist

- [ ] Daily report shows 24-hour graph + 7-day table
- [ ] Weekly report shows 7-day graph + 31-day table
- [ ] Monthly report shows 5-week graph + 31-day table
- [ ] Graph images render correctly in Outlook, Gmail, Apple Mail
- [ ] Plain text fallback shows readable ASCII table
- [ ] Localization works for English and French
- [ ] Empty data periods show zero values
- [ ] Table includes correct total row
