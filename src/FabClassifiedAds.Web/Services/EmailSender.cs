namespace FabClassifiedAds.Web.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>
/// Development email sender: writes each message as an .html file into an outbox
/// folder and logs it. Swap for an SMTP/SES implementation in production.
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
        logger.LogInformation("Email queued to {To}: {Subject} -> {File}", to, subject, file);
    }
}
