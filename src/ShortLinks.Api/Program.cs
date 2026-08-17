using Microsoft.EntityFrameworkCore;
using ShortLinks.Api.Data;
using ShortLinks.Api.Endpoints;
using ShortLinks.Api.Middleware;
using ShortLinks.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration -------------------------------------------------------
var redisConnection = builder.Configuration.GetConnectionString("Redis");

// ---- Persistence ---------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SqlServer")
        ?? "Server=localhost;Database=ShortLinks;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
    options.UseSqlServer(connectionString, sql =>
        sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
});

// ---- Fast cache layer: Redis when configured, in-memory otherwise ----------
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(o =>
    {
        o.Configuration = redisConnection;
        o.InstanceName = builder.Configuration["Cache:InstanceName"] ?? "shortlinks:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// ---- Application services -------------------------------------------------
builder.Services.Configure<PublicOptions>(builder.Configuration.GetSection("Public"));

builder.Services.AddSingleton<ShortCodeGenerator>();
builder.Services.AddSingleton<CacheService>();
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.Section));
builder.Services.AddSingleton<ClickStatsQueue>();
builder.Services.AddHostedService<ClickStatsProcessor>();

builder.Services.AddScoped<AppDbContext>();
builder.Services.AddScoped<LinkQueryService>();
builder.Services.AddScoped<RedirectService>();
builder.Services.AddScoped<LinkManagementService>();
builder.Services.AddScoped<GroupManagementService>();
builder.Services.AddScoped<StatsService>();
builder.Services.AddSingleton<PageRenderer>();

var app = builder.Build();

// ---- Database initialization ----------------------------------------------
if (builder.Configuration.GetValue("Migrate:OnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ---- Pipeline -------------------------------------------------------------
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseStaticFiles();

app.UseRouting();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", utc = DateTimeOffset.UtcNow }));

app.MapRedirectEndpoints();
app.MapLinkEndpoints();
app.MapGroupEndpoints();
app.MapStatsEndpoints();

app.Run();