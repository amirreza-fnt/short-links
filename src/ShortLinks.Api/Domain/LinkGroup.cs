using System.Text.Json;

namespace ShortLinks.Api.Domain;

/// <summary>
/// A named template that appends extra query-string (UTM/SEO) parameters when a
/// short link is requested through it. Example: <c>https://sbzl.ir/utm/154dA</c>
/// where "utm" is the group name and its parameters are merged into the target URL.
/// </summary>
public sealed class LinkGroup
{
    public long Id { get; set; }

    /// <summary>URL-segment name, e.g. "utm", "u1", "u2", "billboard".</summary>
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Serialized key/value parameters appended to the target URL.</summary>
    public string UtmParamsJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<ShortLink> Links { get; set; } = new List<ShortLink>();

    public Dictionary<string, string> GetUtmParams() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(UtmParamsJson)
        ?? new Dictionary<string, string>();

    public void SetUtmParams(Dictionary<string, string> utmParams) =>
        UtmParamsJson = JsonSerializer.Serialize(utmParams);
}