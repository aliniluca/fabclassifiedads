using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FabClassifiedAds.Web.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>Resolved SMTP configuration (from Smtp:* config or SMTP_* env vars).</summary>
public class SmtpOptions
{
    public string? Host { get; init; }
    public int Port { get; init; } = 587;
    public string? User { get; init; }
    public string? Password { get; init; }
    public string From { get; init; } = "no-reply@aicigasesti.ro";
    public string FromName { get; init; } = "AiciGăsești";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);

    public static SmtpOptions FromConfig(IConfiguration c)
    {
        string? V(string key, string env) => c[key] ?? Environment.GetEnvironmentVariable(env);
        return new SmtpOptions
        {
            Host = V("Smtp:Host", "SMTP_HOST"),
            Port = int.TryParse(V("Smtp:Port", "SMTP_PORT"), out var p) ? p : 587,
            User = V("Smtp:User", "SMTP_USER"),
            Password = V("Smtp:Password", "SMTP_PASSWORD"),
            From = V("Smtp:From", "SMTP_FROM") ?? "no-reply@aicigasesti.ro",
            FromName = V("Smtp:FromName", "SMTP_FROM_NAME") ?? "AiciGăsești",
        };
    }
}

/// <summary>Production email sender over SMTP (MailKit). Chooses TLS mode from the port.</summary>
public class SmtpEmailSender(SmtpOptions options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(options.FromName, options.From));
        msg.To.Add(MailboxAddress.Parse(to));
        msg.Subject = subject;
        msg.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        var security = options.Port switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 or 25 => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.Auto,
        };

        using var client = new SmtpClient();
        await client.ConnectAsync(options.Host!, options.Port, security, ct);   // Host is non-null when IsConfigured
        if (!string.IsNullOrEmpty(options.User))
            await client.AuthenticateAsync(options.User, options.Password ?? "", ct);
        await client.SendAsync(msg, ct);
        await client.DisconnectAsync(true, ct);
        logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }
}

/// <summary>
/// Development fallback: writes each message as an .html file into an outbox folder.
/// Used automatically when no SMTP host is configured.
/// </summary>
public class OutboxEmailSender(IWebHostEnvironment env, ILogger<OutboxEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var outbox = Path.Combine(env.ContentRootPath, "App_Data", "outbox");
        Directory.CreateDirectory(outbox);
        var file = Path.Combine(outbox, $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N}.html");
        var content = $"<!-- To: {to} -->\n<!-- Subject: {subject} -->\n{htmlBody}";
        await File.WriteAllTextAsync(file, content, ct);
        logger.LogInformation("Email queued (dev outbox) to {To}: {Subject} -> {File}", to, subject, file);
    }
}
