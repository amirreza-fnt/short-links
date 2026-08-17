using System.Text.RegularExpressions;

namespace ShortLinks.Api.Services;

/// <summary>Validates and normalizes user-supplied target links.</summary>
public static partial class UrlNormalizer
{
    public static bool TryNormalize(string? rawUrl, out string normalizedUrl, out string? error)
    {
        normalizedUrl = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            error = "URL required.";
            return false;
        }

        var candidate = rawUrl.Trim();
        if (candidate.Length > 2048)
        {
            error = "URL is too long (max 2048 characters).";
            return false;
        }

        if (!candidate.Contains("://", StringComparison.Ordinal))
        {
            candidate = "https://" + candidate;
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            error = "URL must use http or https scheme.";
            return false;
        }

        // Guard against obvious injection-style URLs.
        if (ControlCharsRegex().IsMatch(candidate))
        {
            error = "URL contains invalid characters.";
            return false;
        }

        normalizedUrl = uri.AbsoluteUri;
        return true;
    }

    [GeneratedRegex(@"[\u0000-\u001F]")]
    private static partial Regex ControlCharsRegex();
}