using Microsoft.AspNetCore.Mvc;
using ShortLinks.Api.Dtos;
using ShortLinks.Api.Services;

namespace ShortLinks.Api.Endpoints;

public static class GroupEndpoints
{
    public static void MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/groups").WithTags("Groups");

        group.MapPost("/", async (
            CreateGroupRequest request,
            GroupManagementService service,
            HttpRequest http,
            PageRenderer pages,
            CancellationToken ct) =>
        {
            var baseUrl = pages.ResolvePublicBaseUrl(http)?.ToString() ?? string.Empty;
            var created = await service.CreateAsync(request, baseUrl, ct);
            return Results.Created($"/api/groups/{created.Name}", created);
        });

        group.MapGet("/", async (
            GroupManagementService service,
            HttpRequest http,
            PageRenderer pages,
            CancellationToken ct) =>
        {
            var baseUrl = pages.ResolvePublicBaseUrl(http)?.ToString() ?? string.Empty;
            return Results.Ok(await service.GetListAsync(baseUrl, ct));
        });

        group.MapGet("/{name}", async (
            string name,
            GroupManagementService service,
            HttpRequest http,
            PageRenderer pages,
            CancellationToken ct) =>
        {
            var baseUrl = pages.ResolvePublicBaseUrl(http)?.ToString() ?? string.Empty;
            var item = await service.GetAsync(name, baseUrl, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        group.MapPut("/{name}", async (
            string name,
            UpdateGroupRequest request,
            GroupManagementService service,
            HttpRequest http,
            PageRenderer pages,
            CancellationToken ct) =>
        {
            var baseUrl = pages.ResolvePublicBaseUrl(http)?.ToString() ?? string.Empty;
            var updated = await service.UpdateAsync(name, request, baseUrl, ct);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        group.MapDelete("/{name}", async (
            string name,
            GroupManagementService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(name, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        });
    }
}