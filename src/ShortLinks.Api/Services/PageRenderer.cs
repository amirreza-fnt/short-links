using Microsoft.Extensions.Options;

namespace ShortLinks.Api.Services;

public sealed class PublicOptions
{
    public string BaseUrl { get; set; } = string.Empty;
}

/// <summary>
/// Caches the small HTML fallback pages (landing / not-found / unavailable)
/// so repeated error responses do not hit the disk on the hot path.
/// </summary>
public sealed class PageRenderer(
    IWebHostEnvironment env,
    IOptions<PublicOptions> options,
    ILogger<PageRenderer> logger)
{
    private const string LandingFile = "wwwroot/index.html";
    private const string NotFoundFile = "wwwroot/notfound.html";
    private const string UnavailableFile = "wwwroot/unavailable.html";

    private string? _landing;
    private string? _notFound;
    private string? _unavailable;

    public string Landing => GetOrLoad(LandingFile, ref _landing);
    public string NotFound => GetOrLoad(NotFoundFile, ref _notFound);
    public string Unavailable => GetOrLoad(UnavailableFile, ref _unavailable);

    private string GetOrLoad(string relativePath, ref string? field)
    {
        if (field is not null)
        {
            return field;
        }

        var fullPath = Path.Combine(env.ContentRootPath, relativePath);
        if (!File.Exists(fullPath))
        {
            logger.LogWarning("Missing fallback page {Path}", relativePath);
            return "<!doctype html><html><body><h1>Not Found</h1></body></html>";
        }

        field = File.ReadAllText(fullPath);
        return field;
    }

    public Uri ResolvePublicBaseUrl(HttpRequest request)
    {
        var configured = options.Value.BaseUrl?.Trim().TrimEnd('/');
        if (TryGetPublicDomain(configured, out var uri))
            return uri;

        return new Uri("https://sbzl.ir");
    }

    private static bool TryGetPublicDomain(string? url, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var parsed))
            return false;

        if (parsed.Host is "localhost" or "127.0.0.1" or "::1")
            return false;

        if (System.Net.IPAddress.TryParse(parsed.Host, out _))
            return false;

        uri = parsed;
        return true;
    }
}