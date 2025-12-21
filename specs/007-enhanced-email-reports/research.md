# Research: Enhanced Email Reports

**Feature**: 007-enhanced-email-reports
**Date**: 2025-12-21

## Research Questions

### 1. ScottPlot Headless Rendering (No UI)

**Question**: Can ScottPlot render graphs to PNG without requiring a Windows Forms control?

**Decision**: Yes, use `ScottPlot.Plot` directly without `FormsPlot`

**Rationale**: ScottPlot's `Plot` class is independent of WinForms and can render to byte arrays or files:
```csharp
var plot = new ScottPlot.Plot(600, 400);
plot.Add.Scatter(x, y);
byte[] pngBytes = plot.GetImageBytes(600, 400);
// or: plot.SavePng("graph.png", 600, 400);
```

**Alternatives considered**:
- FormsPlot with hidden control: Rejected - unnecessary UI dependency
- External charting library: Rejected - violates Lightweight First principle

### 2. Email Inline Image Embedding with MailKit

**Question**: How to embed PNG images inline in HTML emails using MailKit?

**Decision**: Use `LinkedResource` with Content-ID (CID) references

**Rationale**: MailKit supports MIME multipart/related for inline images:
```csharp
var builder = new BodyBuilder();
var image = builder.LinkedResources.Add("graph.png", pngBytes, ContentType.Parse("image/png"));
image.ContentId = MimeUtils.GenerateMessageId();
builder.HtmlBody = $"<img src='cid:{image.ContentId}' />";
```

**Alternatives considered**:
- Base64 data URLs: Works but increases email size and some clients block them
- External hosted images: Requires server infrastructure, privacy concerns

### 3. Week Aggregation for Monthly Reports

**Question**: How to aggregate data by calendar week (Mon-Sun) for the 5-week graph?

**Decision**: Extend `TimeGranularity` enum to include `Week`, implement in `UsageAggregator`

**Rationale**: The existing aggregator pattern supports Hour and Day granularity. Week follows the same pattern:
```csharp
public enum TimeGranularity { Minute, Hour, Day, Week, Month, Year }

// In UsageAggregator.GetDataPointsAsync:
case TimeGranularity.Week:
    // Group by ISO week (Monday start)
    return summaries.GroupBy(s => ISOWeek.GetWeekOfYear(s.GetPeriodStart()))
        .Select(g => new UsageDataPoint(...));
```

**Alternatives considered**:
- Custom weekly aggregation in ReportGenerator: Rejected - duplicates aggregation logic
- Rolling 7-day windows: Rejected - spec requires calendar weeks (Mon-Sun)

### 4. Table Data Structure

**Question**: What data structure to use for the report table with 7/31 days?

**Decision**: Use existing `UsageSummary` grouped by day, with peak speeds from `GetFilteredPeakSpeedsAsync`

**Rationale**: The existing repository and aggregator already provide:
- `GetSummariesAsync(from, to)` returns hourly summaries
- Group by day and sum totals for daily MB values
- `GetFilteredPeakSpeedsAsync` per day for peak Mbps values

**Alternatives considered**:
- New DailyReport model: Rejected - unnecessary abstraction, UsageSummary suffices
- Precomputed daily aggregates in DB: Rejected - adds storage overhead, aggregation is fast

### 5. Graph Dimensions and Styling

**Question**: What dimensions and styling for email-embedded graphs?

**Decision**: 600x300 pixels, matching existing UI colors (blue download #4285f4, red upload #ea4335)

**Rationale**:
- 600px width fits standard email client widths
- 300px height provides adequate data visibility without scrolling
- Colors match existing UI for brand consistency (confirmed in ReportGenerator.cs)

**Alternatives considered**:
- Larger images: Increases email size, may be clipped on mobile
- SVG format: Poor email client support

### 6. Plain Text Fallback for Graphs

**Question**: How to represent graph data in plain text emails?

**Decision**: ASCII summary table showing period totals

**Rationale**: Plain text emails cannot display images. Provide a compact text summary:
```
HOURLY USAGE (Today)
Hour    Download    Upload
00:00      12 MB      3 MB
01:00       8 MB      2 MB
...
```

**Alternatives considered**:
- ASCII art charts: Hard to read, inconsistent rendering across clients
- Link to web dashboard: Requires external infrastructure

## Summary

All technical questions resolved. Implementation will:
1. Create `EmailReportGraphRenderer` using headless ScottPlot
2. Extend `TimeGranularity` to include `Week`
3. Modify `EmailSender` to support `LinkedResource` inline images
4. Enhance `ReportGenerator` with graph and table generation per frequency
5. Add localization strings for new table headers
