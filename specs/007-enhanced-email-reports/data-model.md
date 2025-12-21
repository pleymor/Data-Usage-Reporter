# Data Model: Enhanced Email Reports

**Feature**: 007-enhanced-email-reports
**Date**: 2025-12-21

## Entities

### Existing Entities (No Changes)

#### UsageSummary
Hourly aggregated usage data stored in SQLite.

| Field | Type | Description |
|-------|------|-------------|
| Id | long | Primary key |
| PeriodStart | long | Unix timestamp of hour start |
| PeriodEnd | long | Unix timestamp of hour end |
| TotalDownload | long | Total bytes downloaded |
| TotalUpload | long | Total bytes uploaded |
| PeakDownloadSpeed | long | Peak download speed (bytes/sec) |
| PeakUploadSpeed | long | Peak upload speed (bytes/sec) |
| SampleCount | int | Number of samples in aggregation |

### Extended Enums

#### TimeGranularity (Extended)

```csharp
public enum TimeGranularity
{
    Minute,
    Hour,
    Day,
    Week,    // NEW - for monthly report graphs
    Month,
    Year
}
```

### New Types

#### ReportTableRow
Represents a single row in the email report table.

| Field | Type | Description |
|-------|------|-------------|
| Date | DateTime | The date (or week start for weekly) |
| DownloadMB | double | Total download in megabytes |
| UploadMB | double | Total upload in megabytes |
| PeakDownloadMbps | double | Peak download speed in Mbps |
| PeakUploadMbps | double | Peak upload speed in Mbps |

#### EmailReportContent
Container for generated report content.

| Field | Type | Description |
|-------|------|-------------|
| GraphPngBytes | byte[] | PNG image of the graph |
| GraphContentId | string | MIME Content-ID for inline reference |
| TableRows | List<ReportTableRow> | Table data rows |
| TotalRow | ReportTableRow | Summary row with totals |
| PeriodLabel | string | Localized period description |

## Data Flow

```
┌─────────────────────┐
│   UsageRepository   │
│   (SQLite DB)       │
└──────────┬──────────┘
           │ GetSummariesAsync()
           ▼
┌─────────────────────┐
│   UsageAggregator   │
│                     │
│ - GetDataPointsAsync│──────────────────────┐
│   (Hour/Day/Week)   │                      │
│                     │                      │
│ - GetFilteredPeaks  │──────────┐           │
│   (per day)         │          │           │
└─────────────────────┘          │           │
                                 │           │
           ┌─────────────────────┘           │
           │                                 │
           ▼                                 ▼
┌─────────────────────┐         ┌─────────────────────┐
│  ReportTableRow[]   │         │ EmailReportGraph    │
│                     │         │ Renderer            │
│  - Build table data │         │                     │
│  - Format values    │         │ - ScottPlot.Plot    │
│  - Calculate totals │         │ - Render to PNG     │
└──────────┬──────────┘         └──────────┬──────────┘
           │                               │
           └───────────┬───────────────────┘
                       │
                       ▼
              ┌─────────────────────┐
              │  ReportGenerator    │
              │                     │
              │  - Build HTML body  │
              │  - Embed graph CID  │
              │  - Generate table   │
              │  - Plain text alt   │
              └──────────┬──────────┘
                         │
                         ▼
              ┌─────────────────────┐
              │    EmailSender      │
              │                     │
              │  - LinkedResource   │
              │  - MIME multipart   │
              └─────────────────────┘
```

## Aggregation Rules

### Daily Report (Hourly Graph)
- **Graph**: 24 data points, one per hour (00:00-23:00)
- **Table**: 7 rows, one per day (today - 6 previous days)
- **Granularity**: Hour for graph, Day for table

### Weekly Report (Daily Graph)
- **Graph**: 7 data points, one per day (last 7 days)
- **Table**: 31 rows, one per day (last 31 days)
- **Granularity**: Day for both

### Monthly Report (Weekly Graph)
- **Graph**: 5 data points, one per calendar week (last 5 weeks)
- **Table**: 31 rows, one per day (last 31 days)
- **Granularity**: Week for graph, Day for table

## Value Conversions

| Source | Target | Formula |
|--------|--------|---------|
| bytes → MB | DownloadMB | bytes / 1,048,576 |
| bytes → MB | UploadMB | bytes / 1,048,576 |
| bytes/sec → Mbps | PeakDownloadMbps | (bytes/sec × 8) / 1,000,000 |
| bytes/sec → Mbps | PeakUploadMbps | (bytes/sec × 8) / 1,000,000 |

## Week Definition

Calendar weeks follow ISO 8601:
- Week starts on **Monday**
- Week ends on **Sunday**
- Use `System.Globalization.ISOWeek.GetWeekOfYear()` for consistent week numbering
