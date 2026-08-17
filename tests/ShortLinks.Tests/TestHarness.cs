using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ShortLinks.Api.Data;
using ShortLinks.Api.Services;

namespace ShortLinks.Tests;

/// <summary>Shared in-memory SQL + in-memory cache harness for service tests.</summary>
public sealed class TestHarness : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private CacheService? _sharedCache;

    public AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(_dbName).Options);

    public CacheService SharedCache =>
        _sharedCache ??= new CacheService(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            Options.Create(new CacheOptions()),
            NullLogger<CacheService>.Instance);

    public ClickStatsQueue CreateQueue() => new();

    public LinkQueryService CreateQueryService(AppDbContext db) => new(db, SharedCache);

    public RedirectService CreateRedirectService(AppDbContext db) =>
        new(CreateQueryService(db), CreateQueue(), NullLogger<RedirectService>.Instance);

    public LinkManagementService CreateLinkService(AppDbContext db) =>
        new(db, SharedCache, new ShortCodeGenerator { Length = 6 }, NullLogger<LinkManagementService>.Instance);

    public GroupManagementService CreateGroupService(AppDbContext db) =>
        new(db, SharedCache, NullLogger<GroupManagementService>.Instance);

    public void Dispose()
    {
        using var db = CreateDbContext();
        db.Database.EnsureDeleted();
    }
}