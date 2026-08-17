using Microsoft.EntityFrameworkCore;
using ShortLinks.Api.Data;
using ShortLinks.Api.Dtos;

namespace ShortLinks.Api.Services;

public sealed record StatsQueryResult
{
    public long TotalClicks { get; set; }
    public long UniqueIps { get; set; }
    public DateTimeOffset? FirstClickAt { get; set; }
    public DateTimeOffset? LastClickAt { get; set; }
    public Dictionary<string, long> ByDevice { get; init; } = new();
    public Dictionary<string, long> ByBrowser { get; init; } = new();
    public Dictionary<string, long> ByTemplate { get; init; } = new();
}

public sealed class StatsService(AppDbContext db)
{
    private sealed record StatRow(DateTimeOffset ClickedAt, string? IpAddress, string? DeviceType, string? Browser, string? UtmTemplate);
    public async Task<StatsQueryResult> GetLinkSummaryAsync(
        long linkId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken ct = default)
    {
        var query = db.ClickStats.AsNoTracking().Where(s => s.ShortLinkId == linkId);

        if (from is not null)
        {
            query = query.Where(s => s.ClickedAt >= from);
        }
        if (to is not null)
        {
            query = query.Where(s => s.ClickedAt <= to);
        }

        var rows = await query
            .Select(s => new StatRow(s.ClickedAt, s.IpAddress, s.DeviceType, s.Browser, s.UtmTemplate))
            .ToListAsync(ct);

        return Summarize(rows);
    }

    public async Task<List<ClicksTimeSeriesPoint>> GetLinkTimeSeriesAsync(
        long linkId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string bucket,
        CancellationToken ct = default)
    {
        var query = db.ClickStats.AsNoTracking().Where(s => s.ShortLinkId == linkId);
        if (from is not null)
        {
            query = query.Where(s => s.ClickedAt >= from);
        }
        if (to is not null)
        {
            query = query.Where(s => s.ClickedAt <= to);
        }

        var rows = await query.Select(s => s.ClickedAt).ToListAsync(ct);
        return Bucketize(rows, bucket);
    }

    public async Task<List<ClickStatDto>> GetLinkStatsAsync(
        long linkId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = db.ClickStats.AsNoTracking().Where(s => s.ShortLinkId == linkId);
        if (from is not null)
        {
            query = query.Where(s => s.ClickedAt >= from);
        }
        if (to is not null)
        {
            query = query.Where(s => s.ClickedAt <= to);
        }

        return await query
            .OrderByDescending(s => s.ClickedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new ClickStatDto
            {
                Id = s.Id,
                ClickedAt = s.ClickedAt,
                IpAddress = s.IpAddress,
                DeviceType = s.DeviceType,
                Browser = s.Browser,
                Referrer = s.Referrer,
                UtmTemplate = s.UtmTemplate,
            })
            .ToListAsync(ct);
    }

    public async Task<OverviewStatsDto> GetOverviewAsync(CancellationToken ct = default)
    {
        var totalLinks = await db.ShortLinks.CountAsync(ct);
        var activeLinks = await db.ShortLinks.CountAsync(l => l.IsActive, ct);
        var totalGroups = await db.LinkGroups.CountAsync(ct);
        var totalClicks = await db.ClickStats.CountAsync(ct);
        var last24 = DateTimeOffset.UtcNow.AddHours(-24);
        var clicksLast24 = await db.ClickStats.CountAsync(s => s.ClickedAt >= last24, ct);

        return new OverviewStatsDto
        {
            TotalLinks = totalLinks,
            ActiveLinks = activeLinks,
            TotalGroups = totalGroups,
            TotalClicks = totalClicks,
            ClicksLast24Hours = clicksLast24,
        };
    }

    private static StatsQueryResult Summarize(IReadOnlyCollection<StatRow> rows)
    {
        var result = new StatsQueryResult();
        var ips = new HashSet<string>();
        DateTimeOffset? first = null;
        DateTimeOffset? last = null;

        foreach (var row in rows)
        {
            first = first is null || row.ClickedAt < first ? row.ClickedAt : first;
            last = last is null || row.ClickedAt > last ? row.ClickedAt : last;

            if (!string.IsNullOrWhiteSpace(row.IpAddress))
            {
                ips.Add(row.IpAddress!);
            }

            var device = row.DeviceType ?? "unknown";
            result.ByDevice[device] = result.ByDevice.GetValueOrDefault(device) + 1;

            var browser = row.Browser ?? "unknown";
            result.ByBrowser[browser] = result.ByBrowser.GetValueOrDefault(browser) + 1;

            if (row.UtmTemplate is not null)
            {
                result.ByTemplate[row.UtmTemplate] = result.ByTemplate.GetValueOrDefault(row.UtmTemplate) + 1;
            }
        }

        result.TotalClicks = rows.Count;
        result.UniqueIps = ips.Count;
        result.FirstClickAt = first;
        result.LastClickAt = last;
        return result;
    }

    private static List<ClicksTimeSeriesPoint> Bucketize(IReadOnlyList<DateTimeOffset> timestamps, string bucket)
    {
        var format = bucket.ToLowerInvariant() switch
        {
            "hour" => "yyyy-MM-dd HH:00",
            "week" => "yyyy-Www",
            "month" => "yyyy-MM",
            _ => "yyyy-MM-dd",
        };

        return timestamps
            .GroupBy(t => t.ToString(format, System.Globalization.CultureInfo.InvariantCulture))
            .Select(g => new ClicksTimeSeriesPoint(g.Key, g.LongCount()))
            .OrderBy(p => p.Bucket)
            .ToList();
    }
}