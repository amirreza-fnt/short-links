using Microsoft.EntityFrameworkCore;
using ShortLinks.Api.Data;

namespace ShortLinks.Api.Services;

public enum RedirectStatus
{
    Found,
    NotFound,
    Unavailable, // expired or disabled
}

public sealed record RedirectOutcome(
    RedirectStatus Status,
    long? LinkId,
    string? FinalUrl,
    string? GroupName);

public sealed class LinkQueryService(
    AppDbContext db,
    CacheService cache)
{
    public async Task<CachedLink?> GetLinkByCodeAsync(
        string code,
        CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.Link(code);
        var cached = await cache.GetAsync<CachedLink>(cacheKey, ct);
        if (cached is not null)
        {
            return cached;
        }

        var link = await db.ShortLinks
            .AsNoTracking()
            .Where(l => l.Code == code)
            .Select(l => new CachedLink
            {
                Id = l.Id,
                Code = l.Code,
                TargetUrl = l.TargetUrl,
                IsActive = l.IsActive,
                ExpiresAt = l.ExpiresAt,
                GroupName = l.Group != null ? l.Group.Name : null,
            })
            .FirstOrDefaultAsync(ct);

        if (link is null)
        {
            return null;
        }

        await cache.SetAsync(cacheKey, link, ct: ct);
        return link;
    }

    public async Task<Dictionary<string, string>?> GetGroupParamsAsync(
        string groupName,
        CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.Group(groupName);
        var cached = await cache.GetAsync<Dictionary<string, string>>(cacheKey, ct);
        if (cached is not null)
        {
            return cached;
        }

        var group = await db.LinkGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Name == groupName, ct);

        if (group is null || !group.IsActive)
        {
            return null;
        }

        var utm = group.GetUtmParams();
        await cache.SetAsync(cacheKey, utm, ct: ct);
        return utm;
    }
}

/// <summary>
/// Resolves a short link request and builds the final redirect URL.
/// Route semantics:
///   /{code}         → target URL (caller query string is merged)
///   /{group}/{code} → target URL + group UTM params (caller query string is merged)
/// </summary>
public sealed class RedirectService(
    LinkQueryService queryService,
    ClickStatsQueue statsQueue,
    ILogger<RedirectService> logger)
{
    public async Task<RedirectOutcome> ResolveAsync(
        string code,
        string? groupName,
        string? callerQueryString,
        string? ipAddress,
        string? userAgent,
        string? referrer,
        CancellationToken ct = default)
    {
        // 1) Group/template parameters (when requested through a group route).
        Dictionary<string, string>? groupParams = null;
        if (!string.IsNullOrEmpty(groupName))
        {
            groupParams = await queryService.GetGroupParamsAsync(groupName, ct);
            if (groupParams is null)
            {
                return new RedirectOutcome(RedirectStatus.NotFound, null, null, groupName);
            }
        }

        // 2) Link lookup — served from cache when possible.
        var link = await queryService.GetLinkByCodeAsync(code, ct);
        if (link is null)
        {
            return new RedirectOutcome(RedirectStatus.NotFound, null, null, groupName);
        }

        // 3) Expiry / active checks.
        if (!link.IsActive || link.IsExpired())
        {
            return new RedirectOutcome(RedirectStatus.Unavailable, link.Id, null, groupName);
        }

        // 4) Build the final URL (group + caller params merged).
        var finalUrl = UtmAppender.Append(link.TargetUrl, groupParams, callerQueryString);

        // 5) Fire-and-forget click statistic. Never awaited on the hot path.
        var (deviceType, browser) = DeviceInfoParser.Parse(userAgent);
        var enqueued = statsQueue.TryEnqueue(new Domain.ClickStat
        {
            ShortLinkId = link.Id,
            ClickedAt = DateTimeOffset.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            DeviceType = deviceType,
            Browser = browser,
            Referrer = referrer,
            UtmTemplate = groupName,
            QueryString = string.IsNullOrWhiteSpace(callerQueryString) ? null : callerQueryString,
        });
        if (!enqueued)
        {
            logger.LogWarning("Click stats queue full; dropping stat for link {LinkId}", link.Id);
        }

        return new RedirectOutcome(RedirectStatus.Found, link.Id, finalUrl, groupName);
    }
}