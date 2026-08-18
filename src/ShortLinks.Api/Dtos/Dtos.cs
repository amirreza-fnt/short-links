namespace ShortLinks.Api.Dtos;

public sealed record CreateLinkRequest
{
    public string Url { get; init; } = string.Empty;
    public string? Code { get; init; }
    public string? GroupName { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record BatchCreateLinkItemRequest
{
    public string Url { get; init; } = string.Empty;
    public string? Code { get; init; }
    public string? GroupName { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record BatchCreateLinksRequest
{
    public IReadOnlyList<BatchCreateLinkItemRequest> Items { get; init; } = Array.Empty<BatchCreateLinkItemRequest>();
}

public sealed record BatchCreateLinkResult
{
    public string Url { get; init; } = string.Empty;
    public string? ShortUrl { get; init; }
    public string? Code { get; init; }
    public string? Error { get; init; }
}

public sealed record BatchCreateLinksResponse
{
    public IReadOnlyList<BatchCreateLinkResult> Results { get; init; } = Array.Empty<BatchCreateLinkResult>();
}

public sealed record UpdateLinkRequest
{
    public string? Url { get; init; }
    public string? GroupName { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public bool? IsActive { get; init; }
}

public sealed record LinkDto
{
    public long Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string ShortUrl { get; init; } = string.Empty;
    public string TargetUrl { get; init; } = string.Empty;
    public string? GroupName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public bool IsActive { get; init; }
    public long ClickCount { get; init; }
    public DateTimeOffset? LastRedirectAt { get; init; }
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public sealed record CreateGroupRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Dictionary<string, string> UtmParams { get; init; } = new();
}

public sealed record UpdateGroupRequest
{
    public string? Description { get; init; }
    public Dictionary<string, string>? UtmParams { get; init; }
    public bool? IsActive { get; init; }
}

public sealed record GroupDto
{
    public long Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Dictionary<string, string> UtmParams { get; init; } = new();
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public long LinkCount { get; init; }
    public string TemplateUrl { get; init; } = string.Empty;
}

public sealed record ClickStatDto
{
    public long Id { get; init; }
    public DateTimeOffset ClickedAt { get; init; }
    public string? IpAddress { get; init; }
    public string? DeviceType { get; init; }
    public string? Browser { get; init; }
    public string? Referrer { get; init; }
    public string? UtmTemplate { get; init; }
}

public sealed record LinkStatsSummaryDto
{
    public long TotalClicks { get; init; }
    public long UniqueIps { get; init; }
    public DateTimeOffset? FirstClickAt { get; init; }
    public DateTimeOffset? LastClickAt { get; init; }
    public IReadOnlyDictionary<string, long> ByDevice { get; init; } = new Dictionary<string, long>();
    public IReadOnlyDictionary<string, long> ByBrowser { get; init; } = new Dictionary<string, long>();
    public IReadOnlyDictionary<string, long> ByTemplate { get; init; } = new Dictionary<string, long>();
}

public sealed record ClicksTimeSeriesPoint(string Bucket, long Clicks);

public sealed record OverviewStatsDto
{
    public long TotalLinks { get; init; }
    public long ActiveLinks { get; init; }
    public long TotalClicks { get; init; }
    public long ClicksLast24Hours { get; init; }
    public long TotalGroups { get; init; }
}