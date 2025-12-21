namespace DataUsageReporter.Email;

/// <summary>
/// Inline attachment for email embedding.
/// </summary>
public record InlineAttachment(
    string ContentId,
    byte[] Data,
    string MimeType,
    string FileName
);
