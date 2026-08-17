namespace ShortLinks.Api.Domain;

/// <summary>A short link (a short code that redirects to a target URL).</summary>
public sealed class ShortLink
{
    public long Id { get; set; }

    /// <summary>Unique short code, e.g. "aB72x". Case-sensitive.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>The (normalized) long target URL.</summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>Optional group this link belongs to.</summary>
    public long? GroupId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Denormalized aggregate click counter, updated asynchronously.</summary>
    public long ClickCount { get; set; }

    public DateTimeOffset? LastRedirectAt { get; set; }

    public LinkGroup? Group { get; set; }

    public ICollection<ClickStat> ClickStats { get; set; } = new List<ClickStat>();
}