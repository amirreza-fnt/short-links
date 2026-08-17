using ShortLinks.Api.Dtos;
using ShortLinks.Api.Services;

namespace ShortLinks.Tests;

public class RedirectFlowTests : IDisposable
{
    private readonly TestHarness _harness = new();

    [Fact]
    public async Task ResolveBase_RedirectsToTargetUrl()
    {
        await using var db = _harness.CreateDbContext();
        var linkService = _harness.CreateLinkService(db);
        var created = await linkService.CreateAsync(
            new CreateLinkRequest { Url = "https://map.sabzevar.ir/A/B/C?Q=1&W=2" },
            "http://sbzl.ir");

        var redirect = _harness.CreateRedirectService(db);
        var outcome = await redirect.ResolveAsync(created.Code, null, null, "127.0.0.1", "curl", null);

        Assert.Equal(RedirectStatus.Found, outcome.Status);
        Assert.Equal("https://map.sabzevar.ir/A/B/C?Q=1&W=2", outcome.FinalUrl);
    }

    [Fact]
    public async Task ResolveWithGroup_MergesUtmParams()
    {
        await using var db = _harness.CreateDbContext();
        var groupService = _harness.CreateGroupService(db);
        await groupService.CreateAsync(new CreateGroupRequest
        {
            Name = "u1",
            UtmParams = new Dictionary<string, string> { ["utm_source"] = "WWW" },
        }, "http://sbzl.ir");

        var linkService = _harness.CreateLinkService(db);
        var created = await linkService.CreateAsync(
            new CreateLinkRequest { Url = "https://map.sabzevar.ir/A/B/C?Q=1&W=2" },
            "http://sbzl.ir");

        var redirect = _harness.CreateRedirectService(db);
        var outcome = await redirect.ResolveAsync(created.Code, "u1", null, "127.0.0.1", "curl", null);

        Assert.Equal(RedirectStatus.Found, outcome.Status);
        Assert.Equal("https://map.sabzevar.ir/A/B/C?Q=1&W=2&utm_source=WWW", outcome.FinalUrl);
    }

    [Fact]
    public async Task ResolveUnknownCode_ReturnsNotFound()
    {
        await using var db = _harness.CreateDbContext();
        var redirect = _harness.CreateRedirectService(db);

        var outcome = await redirect.ResolveAsync("nope99", null, null, "127.0.0.1", "curl", null);

        Assert.Equal(RedirectStatus.NotFound, outcome.Status);
    }

    [Fact]
    public async Task ResolveUnknownGroup_ReturnsNotFound()
    {
        await using var db = _harness.CreateDbContext();
        var linkService = _harness.CreateLinkService(db);
        var created = await linkService.CreateAsync(
            new CreateLinkRequest { Url = "https://example.com" },
            "http://sbzl.ir");

        var redirect = _harness.CreateRedirectService(db);
        var outcome = await redirect.ResolveAsync(created.Code, "missing-group", null, "127.0.0.1", "curl", null);

        Assert.Equal(RedirectStatus.NotFound, outcome.Status);
    }

    [Fact]
    public async Task ResolveExpiredLink_ReturnsUnavailable()
    {
        await using var db = _harness.CreateDbContext();
        var linkService = _harness.CreateLinkService(db);
        var created = await linkService.CreateAsync(new CreateLinkRequest
        {
            Url = "https://example.com",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
        }, "http://sbzl.ir");

        var redirect = _harness.CreateRedirectService(db);
        var outcome = await redirect.ResolveAsync(created.Code, null, null, "127.0.0.1", "curl", null);

        Assert.Equal(RedirectStatus.Unavailable, outcome.Status);
    }

    [Fact]
    public async Task ResolveInactiveLink_ReturnsUnavailable()
    {
        await using var db = _harness.CreateDbContext();
        var linkService = _harness.CreateLinkService(db);
        var created = await linkService.CreateAsync(new CreateLinkRequest
        {
            Url = "https://example.com",
            IsActive = false,
        }, "http://sbzl.ir");

        var redirect = _harness.CreateRedirectService(db);
        var outcome = await redirect.ResolveAsync(created.Code, null, null, "127.0.0.1", "curl", null);

        Assert.Equal(RedirectStatus.Unavailable, outcome.Status);
    }

    [Fact]
    public async Task Resolve_SucceedsWithCallerQueryStringMerged()
    {
        await using var db = _harness.CreateDbContext();
        var linkService = _harness.CreateLinkService(db);
        var created = await linkService.CreateAsync(
            new CreateLinkRequest { Url = "https://example.com/page" },
            "http://sbzl.ir");

        var redirect = _harness.CreateRedirectService(db);
        var outcome = await redirect.ResolveAsync(created.Code, null, "utm_source=POSTER", "127.0.0.1", "curl", null);

        Assert.Equal(RedirectStatus.Found, outcome.Status);
        Assert.Equal("https://example.com/page?utm_source=POSTER", outcome.FinalUrl);
    }

    public void Dispose() => _harness.Dispose();
}