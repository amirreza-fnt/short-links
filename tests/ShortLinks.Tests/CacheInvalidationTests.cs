using Microsoft.EntityFrameworkCore;
using ShortLinks.Api.Dtos;
using ShortLinks.Api.Services;

namespace ShortLinks.Tests;

public class CacheInvalidationTests : IDisposable
{
    private readonly TestHarness _harness = new();

    [Fact]
    public async Task UpdateLink_InvalidatesCache_NextResolveSeesNewUrl()
    {
        await using var db = _harness.CreateDbContext();
        var linkService = _harness.CreateLinkService(db);
        var created = await linkService.CreateAsync(
            new CreateLinkRequest { Url = "https://old.example.com/a" },
            "http://sbzl.ir");

        var redirect = _harness.CreateRedirectService(db);

        // Populate the cache.
        var first = await redirect.ResolveAsync(created.Code, null, null, "1.1.1.1", "curl", null);
        Assert.Equal(RedirectStatus.Found, first.Status);
        Assert.Equal("https://old.example.com/a", first.FinalUrl);

        // Direct DB edit (simulating a background job) should NOT be visible yet.
        var externalEdit = _harness.CreateDbContext();
        var editedLink = await externalEdit.ShortLinks.FirstAsync(l => l.Code == created.Code);
        editedLink.TargetUrl = "https://db.edited.example.com/z";
        await externalEdit.SaveChangesAsync();
        await externalEdit.DisposeAsync();

        var stillCached = await redirect.ResolveAsync(created.Code, null, null, "1.1.1.1", "curl", null);
        Assert.Equal("https://old.example.com/a", stillCached.FinalUrl);

        // Update through the service → cache invalidated → fresh value served.
        await linkService.UpdateAsync(created.Code,
            new UpdateLinkRequest { Url = "https://new.example.com/b" }, "http://sbzl.ir");

        var afterUpdate = await redirect.ResolveAsync(created.Code, null, null, "1.1.1.1", "curl", null);
        Assert.Equal("https://new.example.com/b", afterUpdate.FinalUrl);
    }

    [Fact]
    public async Task DeleteLink_InvalidatesCache_NextResolveReturnsNotFound()
    {
        await using var db = _harness.CreateDbContext();
        var linkService = _harness.CreateLinkService(db);
        var created = await linkService.CreateAsync(
            new CreateLinkRequest { Url = "https://example.com/x" },
            "http://sbzl.ir");

        var redirect = _harness.CreateRedirectService(db);
        await redirect.ResolveAsync(created.Code, null, null, "1.1.1.1", "curl", null); // cache warm

        await linkService.DeleteAsync(created.Code);

        var afterDelete = await redirect.ResolveAsync(created.Code, null, null, "1.1.1.1", "curl", null);
        Assert.Equal(RedirectStatus.NotFound, afterDelete.Status);
    }

    [Fact]
    public async Task UpdateGroup_InvalidatesGroupParamsCache()
    {
        await using var db = _harness.CreateDbContext();
        var groupService = _harness.CreateGroupService(db);
        await groupService.CreateAsync(new CreateGroupRequest
        {
            Name = "u1",
            UtmParams = new Dictionary<string, string> { ["utm_source"] = "OLD" },
        }, "http://sbzl.ir");

        var linkService = _harness.CreateLinkService(db);
        var created = await linkService.CreateAsync(
            new CreateLinkRequest { Url = "https://example.com" },
            "http://sbzl.ir");

        var redirect = _harness.CreateRedirectService(db);

        var before = await redirect.ResolveAsync(created.Code, "u1", null, "1.1.1.1", "curl", null);
        Assert.Equal("https://example.com/?utm_source=OLD", before.FinalUrl);

        await groupService.UpdateAsync("u1", new UpdateGroupRequest
        {
            UtmParams = new Dictionary<string, string> { ["utm_source"] = "NEW" },
        }, "http://sbzl.ir");

        var after = await redirect.ResolveAsync(created.Code, "u1", null, "1.1.1.1", "curl", null);
        Assert.Equal("https://example.com/?utm_source=NEW", after.FinalUrl);
    }

    public void Dispose() => _harness.Dispose();
}