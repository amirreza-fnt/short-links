using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace ShortLinks.Api.Services;

public sealed class CacheOptions
{
    public const string Section = "Cache";

    public string InstanceName { get; set; } = "shortlinks:";
    public double TtlMinutes { get; set; } = 1440;
}

/// <summary>
/// Thin JSON wrapper around <see cref="IDistributedCache"/>. The underlying
/// implementation is chosen at startup (Redis when configured, in-memory
/// otherwise) so running instances share one fast lookup layer.
/// </summary>
public sealed class CacheService(
    IDistributedCache cache,
    IOptions<CacheOptions> options,
    ILogger<CacheService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var bytes = await cache.GetAsync(key, ct);
            if (bytes is null)
            {
                return default;
            }
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }
        catch (Exception ex)
        {
            // A cache failure must never break the redirect flow.
            logger.LogWarning(ex, "Cache read failed for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            await cache.SetAsync(key, bytes, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl ?? TimeSpan.FromMinutes(options.Value.TtlMinutes),
            }, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache write failed for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await cache.RemoveAsync(key, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache removal failed for key {Key}", key);
        }
    }

    public static string Encode(object value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions)));
}