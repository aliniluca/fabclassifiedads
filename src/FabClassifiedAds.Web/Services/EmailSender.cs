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
/// Mailgun configuration (Mailgun:* in appsettings, each value overridable by MAILGUN_* env vars).
/// Keep the API key out of git: set MAILGUN_API_KEY on the server — the repo is public.
/// </summary>
public class MailgunOptions
{
    public string? ApiKey { get; init; }
    public string? Domain { get; init; }
    /// <summary>"EU" (api.eu.mailgun.net) or "US" (api.mailgun.net) — must match the region the domain was created in.</summary>
    public string Region { get; init; } = "EU";
    public string From { get; init; } = "no-reply@aicigasesti.ro";
    public string FromName { get; init; } = "AiciGăsești";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Domain);
    public Uri BaseAddress => new(Region.Equals("US", StringComparison.OrdinalIgnoreCase)
        ? "https://api.mailgun.net/" : "https://api.eu.mailgun.net/");

    public static MailgunOptions FromConfig(IConfiguration c)
    {
        string? V(string key, string env) =>
            Environment.GetEnvironmentVariable(env) is { Length: > 0 } fromEnv ? fromEnv
            : c[key] is { Length: > 0 } fromConfig ? fromConfig : null;
        return new MailgunOptions
        {
            ApiKey = V("Mailgun:ApiKey", "MAILGUN_API_KEY"),
            Domain = V("Mailgun:Domain", "MAILGUN_DOMAIN"),
            Region = V("Mailgun:Region", "MAILGUN_REGION") ?? "EU",
            From = V("Mailgun:From", "MAILGUN_FROM") ?? "no-reply@aicigasesti.ro",
            FromName = V("Mailgun:FromName", "MAILGUN_FROM_NAME") ?? "AiciGăsești",
        };
    }
}

/// <summary>Transactional email over the Mailgun HTTP API (POST /v3/{domain}/messages).</summary>
public class MailgunEmailSender(HttpClient http, MailgunOptions options, ILogger<MailgunEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["from"] = $"{options.FromName} <{options.From}>",
            ["to"] = to,
            ["subject"] = subject,
            ["html"] = htmlBody,
        });
        using var response = await http.PostAsync($"v3/{options.Domain}/messages", form, ct);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Mailgun rejected email to {To} ({Status}): {Error}", to, (int)response.StatusCode, error);
            throw new InvalidOperationException($"Mailgun send failed with HTTP {(int)response.StatusCode}.");
        }
        logger.LogInformation("Email sent via Mailgun to {To}: {Subject}", to, subject);
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
