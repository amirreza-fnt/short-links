using Microsoft.EntityFrameworkCore;
using ShortLinks.Api.Data;
using ShortLinks.Api.Endpoints;
using ShortLinks.Api.Middleware;
using ShortLinks.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var hostingRole = (builder.Configuration["Hosting:Role"] ?? "All").Trim();
var runApi = hostingRole.Equals("Api", StringComparison.OrdinalIgnoreCase)
             || hostingRole.Equals("All", StringComparison.OrdinalIgnoreCase);
var runWeb = hostingRole.Equals("Web", StringComparison.OrdinalIgnoreCase)
             || hostingRole.Equals("All", StringComparison.OrdinalIgnoreCase);

var redisConnection = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SqlServer")
        ?? "Server=localhost;Database=ShortLinks;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true";
    options.UseSqlServer(connectionString, sql =>
        sql.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null));
});

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

builder.Services.Configure<PublicOptions>(builder.Configuration.GetSection("Public"));
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.Section));

builder.Services.AddSingleton<ShortCodeGenerator>();
builder.Services.AddSingleton<CacheService>();
builder.Services.AddSingleton<ClickStatsQueue>();
builder.Services.AddSingleton<PageRenderer>();

builder.Services.AddScoped<LinkQueryService>();
builder.Services.AddScoped<RedirectService>();
builder.Services.AddScoped<LinkManagementService>();
builder.Services.AddScoped<GroupManagementService>();
builder.Services.AddScoped<StatsService>();

if (runWeb)
{
    builder.Services.AddHostedService<ClickStatsProcessor>();
}

if (runApi)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "ShortLinks API",
            Version = "v1",
            Description = "بک‌اند سامانه لینک کوتاه شهرداری سبزوار"
        });
    });
}

var app = builder.Build();

if (runApi && builder.Configuration.GetValue("Migrate:OnStartup", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseMiddleware<ErrorHandlingMiddleware>();

if (runWeb)
{
    app.UseStaticFiles();
}

app.UseRouting();

if (runApi)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ShortLinks API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapMethods("/health", ["GET", "HEAD"], () => Results.Ok(new
{
    status = "healthy",
    role = hostingRole,
    utc = DateTimeOffset.UtcNow
}));

if (runWeb)
{
    app.MapRedirectEndpoints();
}

if (runApi)
{
    app.MapLinkEndpoints();
    app.MapGroupEndpoints();
    app.MapStatsEndpoints();
}

app.Logger.LogInformation("ShortLinks started as {Role} (api={RunApi}, web={RunWeb})", hostingRole, runApi, runWeb);

app.Run();
