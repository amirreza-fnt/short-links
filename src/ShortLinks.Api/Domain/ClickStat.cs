namespace ShortLinks.Api.Domain;

/// <summary>
/// A single redirect/click event. Written asynchronously (never on the redirect
/// hot path) into the analytics table by a background worker.
/// </summary>
public sealed class ClickStat
{
    public long Id { get; set; }

    public long ShortLinkId { get; set; }

    public DateTimeOffset ClickedAt { get; set; }

    /// <summary>Client IPv4/IPv6 address (best effort).</summary>
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? DeviceType { get; set; }

    public string? Browser { get; set; }

    public string? Referrer { get; set; }

    /// <summary>Group/template name used for this request, if any ("utm", "u1", ...).</summary>
    public string? UtmTemplate { get; set; }

    /// <summary>Extra query string supplied by the caller, if any.</summary>
    public string? QueryString { get; set; }

    public ShortLink? ShortLink { get; set; }
}