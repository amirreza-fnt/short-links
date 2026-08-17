using Microsoft.AspNetCore.WebUtilities;

namespace ShortLinks.Api.Services;

/// <summary>
/// Builds the final redirect URL by merging group (UTM/SEO) parameters and any
/// caller-supplied query string onto the stored target URL.
/// </summary>
public static class UtmAppender
{
    /// <summary>
    /// Merges parameters. Caller-supplied values override group values, and
    /// group values override the original URL values (so a template can override
    /// an existing UTM on a shared map link).
    /// </summary>
    public static string Append(
        string targetUrl,
        IReadOnlyDictionary<string, string>? groupParams,
        string? callerQueryString)
    {
        var builder = new UriBuilder(targetUrl);

        var merged = QueryHelpers.ParseQuery(builder.Query);

        if (groupParams is not null)
        {
            foreach (var (key, value) in groupParams)
            {
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }
                merged[key] = value; // group overrides existing
            }
        }

        if (!string.IsNullOrWhiteSpace(callerQueryString))
        {
            var callerParams = QueryHelpers.ParseQuery(callerQueryString.TrimStart('?'));
            foreach (var (key, value) in callerParams)
            {
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }
                merged[key] = value.ToString(); // caller wins
            }
        }

        builder.Query = QueryHelpers.AddQueryString(string.Empty, merged);
        return builder.Uri.AbsoluteUri;
    }
}