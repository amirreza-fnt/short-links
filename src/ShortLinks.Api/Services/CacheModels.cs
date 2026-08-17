namespace ShortLinks.Api.Services;

/// <summary>Snapshot of a short link kept in the fast cache layer.</summary>
public sealed record CachedLink
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string TargetUrl { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? GroupName { get; init; }

    public bool IsExpired() => ExpiresAt is { } expiry && expiry <= DateTimeOffset.UtcNow;
}

public static class CacheKeys
{
    public static string Link(string code) => $"sl:link:{code}";

    public static string Group(string name) => $"sl:group:{name}";
}