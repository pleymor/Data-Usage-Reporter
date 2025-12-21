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
