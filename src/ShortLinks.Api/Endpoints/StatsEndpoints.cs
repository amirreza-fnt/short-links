using Microsoft.AspNetCore.Mvc;
using ShortLinks.Api.Dtos;
using ShortLinks.Api.Services;

namespace ShortLinks.Api.Endpoints;

public static class StatsEndpoints
{
    public static void MapStatsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Stats");

        group.MapGet("/links/{code}/stats/summary", async (
            string code,
            LinkManagementService links,
            StatsService stats,
            DateTimeOffset? from,
            DateTimeOffset? to,
            CancellationToken ct) =>
        {
            var link = await links.GetAsync(code, string.Empty, ct);
            if (link is null)
            {
                return Results.NotFound();
            }
            var summary = await stats.GetLinkSummaryAsync(link.Id, from, to, ct);
            return Results.Ok(new LinkStatsSummaryDto
            {
                TotalClicks = summary.TotalClicks,
                UniqueIps = summary.UniqueIps,
                FirstClickAt = summary.FirstClickAt,
                LastClickAt = summary.LastClickAt,
                ByDevice = summary.ByDevice,
                ByBrowser = summary.ByBrowser,
                ByTemplate = summary.ByTemplate,
            });
        });

        group.MapGet("/links/{code}/stats", async (
            string code,
            LinkManagementService links,
            StatsService stats,
            DateTimeOffset? from,
            DateTimeOffset? to,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            CancellationToken ct = default) =>
        {
            var link = await links.GetAsync(code, string.Empty, ct);
            if (link is null)
            {
                return Results.NotFound();
            }
            var items = await stats.GetLinkStatsAsync(link.Id, from, to, page, pageSize, ct);
            return Results.Ok(items);
        });

        group.MapGet("/links/{code}/stats/timeseries", async (
            string code,
            LinkManagementService links,
            StatsService stats,
            DateTimeOffset? from,
            DateTimeOffset? to,
            string bucket = "day",
            CancellationToken ct = default) =>
        {
            var link = await links.GetAsync(code, string.Empty, ct);
            if (link is null)
            {
                return Results.NotFound();
            }
            var series = await stats.GetLinkTimeSeriesAsync(link.Id, from, to, bucket, ct);
            return Results.Ok(series);
        });

        group.MapGet("/stats/overview", async (StatsService stats, CancellationToken ct) =>
            Results.Ok(await stats.GetOverviewAsync(ct)));
    }
}