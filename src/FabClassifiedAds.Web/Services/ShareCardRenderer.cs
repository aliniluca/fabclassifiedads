using FabClassifiedAds.Web.Helpers;
using FabClassifiedAds.Web.Models.Entities;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace FabClassifiedAds.Web.Services;

/// <summary>
/// Renders the 1080x1920 (9:16) share card for TikTok / Instagram stories as a JPEG, in the site's
/// inspection-sheet style: navy band with the wordmark, the cover photo (or the brand plate),
/// a white sheet with title, specs, price and the AiciGăsești check strip.
/// Pure managed code (ImageSharp) — no native libraries on the server.
/// </summary>
public class ShareCardRenderer
{
    private const int W = 1080, H = 1920, Pad = 60, Inner = W - 2 * Pad;

    private static readonly Color Ink = Color.ParseHex("13233a");
    private static readonly Color Muted = Color.ParseHex("556274");
    private static readonly Color Page = Color.ParseHex("f3f5f7");
    private static readonly Color Line = Color.ParseHex("d6dce3");
    private static readonly Color PlateGround = Color.ParseHex("e9edf1");
    private static readonly Color PlateInk = Color.ParseHex("7d8a99");
    private static readonly Color Mint = Color.ParseHex("7fd1ad");
    private static readonly Color Green = Color.ParseHex("0b6b4f");
    private static readonly Color GreenSoft = Color.ParseHex("e7f1ec");
    private static readonly Color Amber = Color.ParseHex("8f5400");
    private static readonly Color AmberSoft = Color.ParseHex("fdf3e1");
    private static readonly Color Red = Color.ParseHex("c62828");
    private static readonly Color RedSoft = Color.ParseHex("fdeceb");

    private readonly FontFamily regular, semiBold, bold, extraBold;
    private readonly IWebHostEnvironment env;

    public ShareCardRenderer(IWebHostEnvironment env)
    {
        this.env = env;
        var dir = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "fonts");
        var fonts = new FontCollection();
        regular = fonts.Add(System.IO.Path.Combine(dir, "PublicSans-Regular.ttf"));
        semiBold = fonts.Add(System.IO.Path.Combine(dir, "PublicSans-SemiBold.ttf"));
        bold = fonts.Add(System.IO.Path.Combine(dir, "PublicSans-Bold.ttf"));
        extraBold = fonts.Add(System.IO.Path.Combine(dir, "PublicSans-ExtraBold.ttf"));
    }

    public async Task<byte[]> RenderAsync(Listing listing, string categoryName, string shortUrl)
    {
        using var image = new Image<Rgba32>(W, H, Page.ToPixel<Rgba32>());
        using var photo = await LoadCoverAsync(listing);

        image.Mutate(ctx =>
        {
            // navy band with the stamp mark + wordmark
            ctx.Fill(Ink, new RectangularPolygon(0, 0, W, 200));
            DrawMark(ctx, Pad + 34, 100, 34, Mint);
            var word = new RichTextOptions(bold.CreateFont(64, FontStyle.Bold)) { Origin = new PointF(Pad + 86, 100), VerticalAlignment = VerticalAlignment.Center };
            ctx.DrawText(word, "Aici", Color.White);
            var aiciWidth = TextMeasurer.MeasureAdvance("Aici", new TextOptions(bold.CreateFont(64, FontStyle.Bold))).Width;
            ctx.DrawText(new RichTextOptions(regular.CreateFont(64)) { Origin = new PointF(Pad + 86 + aiciWidth, 100), VerticalAlignment = VerticalAlignment.Center },
                "Găsești", Color.White);

            // cover photo or brand plate, 960x880
            const int photoY = 240, photoH = 880;
            if (photo is not null)
            {
                ctx.DrawImage(photo, new Point(Pad, photoY), 1f);
            }
            else
            {
                ctx.Fill(PlateGround, new RectangularPolygon(Pad, photoY, Inner, photoH));
                DrawMark(ctx, W / 2f, photoY + photoH / 2f - 50, 70, PlateInk);
                ctx.DrawText(new RichTextOptions(semiBold.CreateFont(52))
                {
                    Origin = new PointF(W / 2f, photoY + photoH / 2f + 80),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                }, "AiciGăsești", PlateInk);
            }
            ctx.Draw(Line, 2, new RectangularPolygon(Pad, photoY, Inner, photoH));

            // white sheet: category, title, specs, price, check strip
            const int sheetY = photoY + photoH + 40, sheetH = 560;
            ctx.Fill(Color.White, new RectangularPolygon(Pad, sheetY, Inner, sheetH));
            ctx.Draw(Ink, 3, new RectangularPolygon(Pad, sheetY, Inner, sheetH));

            float x = Pad + 44, y = sheetY + 40, textWidth = Inner - 88;
            y = DrawLines(ctx, Wrap(categoryName.ToUpperInvariant(), semiBold.CreateFont(30), textWidth, 1), semiBold.CreateFont(30), Muted, x, y, 40) + 14;
            y = DrawLines(ctx, Wrap(listing.Title, bold.CreateFont(56, FontStyle.Bold), textWidth, 2), bold.CreateFont(56, FontStyle.Bold), Ink, x, y, 68) + 10;
            var summary = listing.ListingSummary();
            if (!string.IsNullOrWhiteSpace(summary))
                y = DrawLines(ctx, Wrap(summary, regular.CreateFont(34), textWidth, 1), regular.CreateFont(34), Muted, x, y, 44);

            // price sits just above the strip, whatever the title length
            const int stripH = 96;
            var stripY = sheetY + sheetH - stripH;
            ctx.DrawText(new RichTextOptions(extraBold.CreateFont(92)) { Origin = new PointF(x, stripY - 34), VerticalAlignment = VerticalAlignment.Bottom },
                listing.Price(), Ink);

            var (fg, bg, label) = listing.TrustScore >= 75 ? (Green, GreenSoft, Translator.IsRo ? "VERIFICAT AICIGĂSEȘTI" : "CHECKED BY AICIGĂSEȘTI")
                : listing.TrustScore >= 40 ? (Amber, AmberSoft, Translator.IsRo ? "ATENȚIE" : "CAUTION")
                : (Red, RedSoft, Translator.IsRo ? "RISC RIDICAT" : "HIGH RISK");
            ctx.Fill(bg, new RectangularPolygon(Pad + 1.5f, stripY, Inner - 3, stripH - 1.5f));
            ctx.DrawLine(Line, 2, new PointF(Pad, stripY), new PointF(Pad + Inner, stripY));
            var mid = stripY + stripH / 2f;
            if (listing.TrustScore >= 75)
                ctx.DrawLine(fg, 7, new PointF(x, mid), new PointF(x + 14, mid + 14), new PointF(x + 40, mid - 16));
            else
                ctx.DrawText(new RichTextOptions(extraBold.CreateFont(44)) { Origin = new PointF(x + 12, mid), VerticalAlignment = VerticalAlignment.Center }, "!", fg);
            ctx.DrawText(new RichTextOptions(bold.CreateFont(32, FontStyle.Bold)) { Origin = new PointF(x + 62, mid), VerticalAlignment = VerticalAlignment.Center }, label, fg);
            ctx.DrawText(new RichTextOptions(extraBold.CreateFont(36))
            {
                Origin = new PointF(Pad + Inner - 44, mid), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center,
            }, $"{listing.TrustScore}/100", fg);

            // footer: city + short link
            ctx.DrawText(new RichTextOptions(semiBold.CreateFont(38))
            {
                Origin = new PointF(W / 2f, H - 70), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            }, $"{listing.City.Place()}  ·  {shortUrl}", Ink);
        });

        await using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 88 });
        return ms.ToArray();
    }

    /// <summary>Loads the first uploaded photo (local files only) cropped to the photo slot.</summary>
    private async Task<Image<Rgba32>?> LoadCoverAsync(Listing listing)
    {
        var url = listing.Images.FirstOrDefault()?.Url;
        if (url is null || !url.StartsWith("/uploads/")) return null;
        var path = System.IO.Path.GetFullPath(System.IO.Path.Combine(env.WebRootPath, url.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar)));
        if (!path.StartsWith(env.WebRootPath, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return null;
        try
        {
            var img = await Image.LoadAsync<Rgba32>(path);
            img.Mutate(x => x.Resize(new ResizeOptions { Size = new Size(Inner, 880), Mode = ResizeMode.Crop }));
            return img;
        }
        catch (Exception) { return null; }   // unreadable/unsupported file -> brand plate
    }

    /// <summary>The AiciGăsești stamp: double ring with a check.</summary>
    private static void DrawMark(IImageProcessingContext ctx, float cx, float cy, float r, Color color)
    {
        ctx.Draw(color, r * 0.16f, new EllipsePolygon(cx, cy, r));
        ctx.Draw(color, r * 0.07f, new EllipsePolygon(cx, cy, r * 0.75f));
        var s = r / 14f;
        ctx.DrawLine(color, r * 0.17f, new PointF(cx - 5 * s, cy + 0.4f * s), new PointF(cx - 1.6f * s, cy + 3.8f * s), new PointF(cx + 5 * s, cy - 3.4f * s));
    }

    private static float DrawLines(IImageProcessingContext ctx, List<string> lines, Font font, Color color, float x, float y, float lineHeight)
    {
        foreach (var line in lines)
        {
            ctx.DrawText(new RichTextOptions(font) { Origin = new PointF(x, y) }, line, color);
            y += lineHeight;
        }
        return y;
    }

    /// <summary>Word-wraps to the pixel width, at most <paramref name="maxLines"/> lines, ellipsis on overflow.</summary>
    private static List<string> Wrap(string text, Font font, float width, int maxLines)
    {
        var options = new TextOptions(font);
        float Measure(string s) => TextMeasurer.MeasureAdvance(s, options).Width;

        var lines = new List<string>();
        var current = "";
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = current.Length == 0 ? word : $"{current} {word}";
            if (Measure(candidate) <= width || current.Length == 0) { current = candidate; continue; }
            lines.Add(current);
            current = word;
            if (lines.Count == maxLines) break;
        }
        if (lines.Count < maxLines && current.Length > 0) { lines.Add(current); current = ""; }

        var truncated = current.Length > 0 || lines.Sum(l => l.Split(' ').Length) < text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (truncated && lines.Count > 0)
        {
            var last = lines[^1];
            while (last.Length > 1 && Measure(last + "…") > width) last = last[..^1].TrimEnd();
            lines[^1] = last + "…";
        }
        // a single word wider than the line (long model codes) is cut, never overflows
        for (var i = 0; i < lines.Count; i++)
            while (lines[i].Length > 1 && Measure(lines[i]) > width) lines[i] = lines[i][..^2] + "…";
        return lines;
    }
}
