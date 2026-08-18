using Microsoft.AspNetCore.Mvc;
using ShortLinks.Api.Dtos;
using ShortLinks.Api.Services;

namespace ShortLinks.Api.Endpoints;

public static class LinkEndpoints
{
    public static void MapLinkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/links").WithTags("Links");

        group.MapPost("/", async (
            CreateLinkRequest request,
            LinkManagementService service,
            HttpRequest http,
            PageRenderer pages,
            CancellationToken ct) =>
        {
            var baseUrl = pages.ResolvePublicBaseUrl(http)?.ToString() ?? string.Empty;
            var created = await service.CreateAsync(request, baseUrl, ct);
            return Results.Created($"/api/links/{created.Code}", created);
        });

        group.MapPost("/batch", async (
            BatchCreateLinksRequest request,
            LinkManagementService service,
            HttpRequest http,
            PageRenderer pages,
            CancellationToken ct) =>
        {
            var baseUrl = pages.ResolvePublicBaseUrl(http)?.ToString() ?? string.Empty;
            var created = await service.CreateBatchAsync(request, baseUrl, ct);
            return Results.Ok(created);
        });

        group.MapGet("/", async (
            LinkManagementService service,
            HttpRequest http,
            PageRenderer pages,
            string? search,
            string? groupName,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default) =>
        {
            var baseUrl = pages.ResolvePublicBaseUrl(http)?.ToString() ?? string.Empty;
            return Results.Ok(await service.GetListAsync(search, groupName, page, pageSize, baseUrl, ct));
        });

        group.MapGet("/{code}", async (
            string code,
            LinkManagementService service,
            HttpRequest http,
            PageRenderer pages,
            CancellationToken ct) =>
        {
            var baseUrl = pages.ResolvePublicBaseUrl(http)?.ToString() ?? string.Empty;
            var link = await service.GetAsync(code, baseUrl, ct);
            return link is null ? Results.NotFound() : Results.Ok(link);
        });

        group.MapPut("/{code}", async (
            string code,
            UpdateLinkRequest request,
            LinkManagementService service,
            HttpRequest http,
            PageRenderer pages,
            CancellationToken ct) =>
        {
            var baseUrl = pages.ResolvePublicBaseUrl(http)?.ToString() ?? string.Empty;
            var updated = await service.UpdateAsync(code, request, baseUrl, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        group.MapDelete("/{code}", async (
            string code,
            LinkManagementService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(code, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        });
    }
}