using System.Net;
using System.Net.Sockets;

namespace FabClassifiedAds.Web.Services;

/// <summary>
/// Fetches user-supplied URLs with SSRF protection. Every actual TCP connection
/// goes through a ConnectCallback that resolves DNS itself and refuses to connect
/// to loopback / private / link-local / cloud-metadata addresses (this also covers
/// redirects and DNS-rebinding, since redirects re-enter the callback). Only http/https
/// on ports 80/443 are allowed, responses are size-capped, and there is a hard timeout.
/// </summary>
public class SafeHttpFetcher
{
    private const string UserAgent =
        "AiciGasestiBot/1.0 (+https://aicigasesti.ro/bot; cross-post import on behalf of the ad owner)";
    private const long MaxHtmlBytes = 3 * 1024 * 1024;
    private const long MaxImageBytes = 8 * 1024 * 1024;

    private readonly HttpClient _client;

    public SafeHttpFetcher()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5,
            AutomaticDecompression = DecompressionMethods.All,
            ConnectTimeout = TimeSpan.FromSeconds(10),
            ConnectCallback = SafeConnectAsync,
        };
        _client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
        _client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        _client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ro,en;q=0.8");
    }

    public bool IsFetchableUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) &&
        (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);

    public async Task<string?> FetchHtmlAsync(string url, CancellationToken ct = default)
    {
        using var resp = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode) return null;
        var media = resp.Content.Headers.ContentType?.MediaType ?? "";
        if (!media.Contains("html", StringComparison.OrdinalIgnoreCase) && media != "") return null;
        var bytes = await ReadCappedAsync(resp, MaxHtmlBytes, ct);
        return bytes is null ? null : System.Text.Encoding.UTF8.GetString(bytes);
    }

    public async Task<(byte[] Data, string ContentType)?> FetchImageAsync(string url, CancellationToken ct = default)
    {
        if (!IsFetchableUrl(url)) return null;
        using var resp = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode) return null;
        var media = resp.Content.Headers.ContentType?.MediaType ?? "";
        if (!media.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return null;
        var bytes = await ReadCappedAsync(resp, MaxImageBytes, ct);
        return bytes is null ? null : (bytes, media.ToLowerInvariant());
    }

    private static async Task<byte[]?> ReadCappedAsync(HttpResponseMessage resp, long cap, CancellationToken ct)
    {
        if (resp.Content.Headers.ContentLength is > 0 && resp.Content.Headers.ContentLength > cap) return null;
        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > cap) return null;      // refuse oversized bodies
            buffer.Write(chunk, 0, read);
        }
        return buffer.ToArray();
    }

    private static async ValueTask<Stream> SafeConnectAsync(SocketsHttpConnectionContext ctx, CancellationToken ct)
    {
        var host = ctx.DnsEndPoint.Host;
        var port = ctx.DnsEndPoint.Port;
        if (port is not (80 or 443))
            throw new IOException($"Blocked port {port} (only 80/443 allowed).");

        var addresses = await Dns.GetHostAddressesAsync(host, ct);
        var target = addresses.FirstOrDefault(a => !IsBlocked(a))
            ?? throw new IOException($"Refusing to connect to a private/reserved address for host '{host}'.");

        var socket = new Socket(target.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(new IPEndPoint(target, port), ct);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    /// <summary>True for addresses we must never connect to (SSRF-sensitive ranges).</summary>
    private static bool IsBlocked(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return true;

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return b[0] == 10                                   // 10.0.0.0/8
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)    // 172.16.0.0/12
                || (b[0] == 192 && b[1] == 168)                 // 192.168.0.0/16
                || (b[0] == 169 && b[1] == 254)                 // 169.254.0.0/16 (incl. cloud metadata)
                || b[0] == 127                                  // 127.0.0.0/8
                || b[0] == 0                                    // 0.0.0.0/8
                || b[0] >= 224;                                 // multicast / reserved
        }

        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6Multicast) return true;
            if (ip.IsIPv4MappedToIPv6) return IsBlocked(ip.MapToIPv4());
            var b = ip.GetAddressBytes();
            if ((b[0] & 0xFE) == 0xFC) return true;             // fc00::/7 unique-local
        }
        return false;
    }
}
