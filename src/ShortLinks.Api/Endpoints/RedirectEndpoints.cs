using ShortLinks.Api.Services;

namespace ShortLinks.Api.Endpoints;

public static class RedirectEndpoints
{
    public static void MapRedirectEndpoints(this IEndpointRouteBuilder app)
    {
        // Landing page at the root.
        app.MapGet("/", (PageRenderer pages) =>
            Results.Content(pages.Landing, "text/html; charset=utf-8"));

        // Legacy favicon request → bundled SVG.
        app.MapGet("/favicon.ico", () => Results.Redirect("/favicon.svg", permanent: true));

        // Localized redirect route: GET/HEAD only; other verbs get 405 automatically.
        app.MapMethods("/{code:regex(^[0-9A-Za-z]+$)}", ["GET", "HEAD"], HandleSingleSegment);
        app.MapMethods("/{group}/{code:regex(^[0-9A-Za-z]+$)}", ["GET", "HEAD"], HandleDoubleSegment);
    }

    private static async Task<IResult> HandleSingleSegment(
        HttpContext http,
        string code,
        RedirectService redirectService,
        PageRenderer pages,
        CancellationToken ct)
    {
        var outcome = await redirectService.ResolveAsync(
            code,
            groupName: null,
            callerQueryString: http.Request.QueryString.Value,
            ipAddress: http.Connection.RemoteIpAddress?.ToString(),
            userAgent: http.Request.Headers.UserAgent.ToString(),
            referrer: http.Request.Headers.Referer.ToString(),
            ct);

        return ToResult(outcome, pages);
    }

    private static async Task<IResult> HandleDoubleSegment(
        HttpContext http,
        string group,
        string code,
        RedirectService redirectService,
        PageRenderer pages,
        CancellationToken ct)
    {
        var outcome = await redirectService.ResolveAsync(
            code,
            groupName: group,
            callerQueryString: http.Request.QueryString.Value,
            ipAddress: http.Connection.RemoteIpAddress?.ToString(),
            userAgent: http.Request.Headers.UserAgent.ToString(),
            referrer: http.Request.Headers.Referer.ToString(),
            ct);

        return ToResult(outcome, pages);
    }

    private static IResult ToResult(RedirectOutcome outcome, PageRenderer pages)
    {
        return outcome.Status switch
        {
            RedirectStatus.Found => Results.Redirect(outcome.FinalUrl!, permanent: false),
            RedirectStatus.Unavailable => Results.Content(
                pages.Unavailable,
                "text/html; charset=utf-8",
                statusCode: StatusCodes.Status410Gone),
            _ => Results.Content(
                pages.NotFound,
                "text/html; charset=utf-8",
                statusCode: StatusCodes.Status404NotFound),
        };
    }
}