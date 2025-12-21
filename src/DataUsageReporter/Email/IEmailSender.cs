namespace DataUsageReporter.Email;

/// <summary>
/// Sends email reports via SMTP.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends an email with the specified content.
    /// </summary>
    /// <returns>True if sent successfully</returns>
    Task<bool> SendAsync(EmailMessage message);

    /// <summary>
    /// Tests the current email configuration.
    /// </summary>
    /// <returns>Validation result with error details if failed</returns>
    Task<ValidationResult> TestConnectionAsync();
}

/// <summary>
/// Email message content with optional inline attachments.
/// </summary>
public record EmailMessage(
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null,
    IReadOnlyList<InlineAttachment>? InlineAttachments = null
);

/// <summary>
/// Validation result for connection tests.
/// </summary>
public record ValidationResult(
    bool IsValid,
    string? ErrorMessage = null
);
