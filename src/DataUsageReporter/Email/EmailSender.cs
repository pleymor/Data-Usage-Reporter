using DnsClient;
using DnsClient.Protocol;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using DataUsageReporter.Data;

namespace DataUsageReporter.Email;

/// <summary>
/// Sends emails using MailKit with TLS/SSL support.
/// Supports both SMTP relay and direct send (MX lookup) modes.
/// </summary>
public class EmailSender : IEmailSender
{
    private readonly EmailConfig _config;
    private readonly ICredentialManager? _credentialManager;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    public EmailSender(EmailConfig config, ICredentialManager? credentialManager = null)
    {
        _config = config;
        _credentialManager = credentialManager;
    }

    /// <summary>
    /// Returns true if SMTP relay is configured, false if using direct send mode.
    /// </summary>
    public bool IsSmtpConfigured => !string.IsNullOrWhiteSpace(_config.SmtpServer);

    public async Task<bool> SendAsync(EmailMessage message)
    {
        // Use SendWithDetailsAsync and just return the boolean result
        var result = await SendWithDetailsAsync(message);
        return result.Success;
    }

    /// <summary>
    /// Sends email and returns detailed result including error message on failure.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage)> SendWithDetailsAsync(EmailMessage message)
    {
        try
        {
            var mimeMessage = CreateMimeMessage(message);

            if (IsSmtpConfigured)
            {
                await SendViaSmtpRelayAsync(mimeMessage);
            }
            else
            {
                await SendDirectAsync(mimeMessage);
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<ValidationResult> TestConnectionAsync()
    {
        try
        {
            if (IsSmtpConfigured)
            {
                return await TestSmtpConnectionAsync();
            }
            else
            {
                return await TestDirectSendAsync();
            }
        }
        catch (Exception ex)
        {
            return new ValidationResult(false, ex.Message);
        }
    }

    private async Task<ValidationResult> TestSmtpConnectionAsync()
    {
        using var client = new SmtpClient();
        client.Timeout = (int)Timeout.TotalMilliseconds;

        var secureSocketOptions = _config.UseSsl
            ? (_config.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
            : SecureSocketOptions.None;

        await client.ConnectAsync(_config.SmtpServer, _config.SmtpPort, secureSocketOptions);

        if (_credentialManager != null)
        {
            var credentials = _credentialManager.Retrieve(_config.CredentialKey);
            if (credentials.HasValue)
            {
                await client.AuthenticateAsync(credentials.Value.Username, credentials.Value.Password);
            }
        }

        await client.DisconnectAsync(true);

        return new ValidationResult(true);
    }

    private async Task<ValidationResult> TestDirectSendAsync()
    {
        // Test MX lookup for recipient domain
        var recipientDomain = GetDomainFromEmail(_config.RecipientEmail);
        if (string.IsNullOrEmpty(recipientDomain))
        {
            return new ValidationResult(false, "Invalid recipient email address");
        }

        var mxRecords = await LookupMxRecordsAsync(recipientDomain);
        if (mxRecords.Count == 0)
        {
            return new ValidationResult(false, $"No MX records found for domain: {recipientDomain}");
        }

        // Try to connect to first MX server
        var mxHost = mxRecords[0];
        using var client = new SmtpClient();
        client.Timeout = (int)Timeout.TotalMilliseconds;

        try
        {
            // Direct send uses port 25 without authentication
            await client.ConnectAsync(mxHost, 25, SecureSocketOptions.None);
            await client.DisconnectAsync(true);
            return new ValidationResult(true, $"Direct send available via {mxHost}");
        }
        catch (Exception ex)
        {
            return new ValidationResult(false, $"Cannot connect to MX server {mxHost}: {ex.Message}. Your ISP may block port 25. Configure SMTP relay instead.");
        }
    }

    private MimeMessage CreateMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        // For direct send, sender should match a valid address or use recipient domain
        var senderEmail = !string.IsNullOrEmpty(_config.SenderEmail)
            ? _config.SenderEmail
            : $"noreply@{GetDomainFromEmail(_config.RecipientEmail)}";

        mimeMessage.From.Add(MailboxAddress.Parse(senderEmail));
        mimeMessage.To.Add(MailboxAddress.Parse(_config.RecipientEmail));
        mimeMessage.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody ?? StripHtml(message.HtmlBody)
        };

        mimeMessage.Body = builder.ToMessageBody();

        return mimeMessage;
    }

    private async Task SendViaSmtpRelayAsync(MimeMessage message)
    {
        using var client = new SmtpClient();
        client.Timeout = (int)Timeout.TotalMilliseconds;

        var secureSocketOptions = _config.UseSsl
            ? (_config.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
            : SecureSocketOptions.None;

        await client.ConnectAsync(_config.SmtpServer, _config.SmtpPort, secureSocketOptions);

        if (_credentialManager != null)
        {
            var credentials = _credentialManager.Retrieve(_config.CredentialKey);
            if (credentials.HasValue)
            {
                await client.AuthenticateAsync(credentials.Value.Username, credentials.Value.Password);
            }
        }

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    private async Task SendDirectAsync(MimeMessage message)
    {
        var recipientAddress = message.To.Mailboxes.First();
        var recipientDomain = GetDomainFromEmail(recipientAddress.Address);

        if (string.IsNullOrEmpty(recipientDomain))
        {
            throw new InvalidOperationException("Invalid recipient email address");
        }

        var mxRecords = await LookupMxRecordsAsync(recipientDomain);
        if (mxRecords.Count == 0)
        {
            throw new InvalidOperationException($"No MX records found for domain: {recipientDomain}");
        }

        Exception? lastException = null;

        // Try each MX server in priority order
        foreach (var mxHost in mxRecords)
        {
            try
            {
                using var client = new SmtpClient();
                client.Timeout = (int)Timeout.TotalMilliseconds;

                // Direct send uses port 25, try with opportunistic TLS
                await client.ConnectAsync(mxHost, 25, SecureSocketOptions.StartTlsWhenAvailable);

                // No authentication for direct send
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return; // Success!
            }
            catch (Exception ex)
            {
                lastException = ex;
                // Try next MX server
            }
        }

        throw new InvalidOperationException(
            $"Failed to send email via direct send. Last error: {lastException?.Message}. " +
            "Your ISP may block port 25. Configure SMTP relay instead.");
    }

    private async Task<List<string>> LookupMxRecordsAsync(string domain)
    {
        var lookup = new LookupClient();
        var result = await lookup.QueryAsync(domain, QueryType.MX);

        return result.Answers
            .OfType<MxRecord>()
            .OrderBy(mx => mx.Preference)
            .Select(mx => mx.Exchange.Value.TrimEnd('.'))
            .ToList();
    }

    private static string? GetDomainFromEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return null;

        var atIndex = email.LastIndexOf('@');
        if (atIndex < 0 || atIndex >= email.Length - 1)
            return null;

        return email.Substring(atIndex + 1);
    }

    private static string StripHtml(string html)
    {
        // Simple HTML to text conversion
        var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
        return text.Trim();
    }
}
