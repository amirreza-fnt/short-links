using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using ShortLinks.Api.Data;
using ShortLinks.Api.Domain;

namespace ShortLinks.Api.Services;

/// <summary>
/// Drains the click queue in batches and persists to the analytics tables.
/// Performs a single batched insert plus one aggregate update per affected link,
/// so click logging never blocks a redirect.
/// </summary>
public sealed class ClickStatsProcessor(
    IServiceScopeFactory scopeFactory,
    ClickStatsQueue queue,
    ILogger<ClickStatsProcessor> logger)
    : BackgroundService
{
    private const int MaxBatchSize = 200;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Click stats processor started");
        var buffer = new List<ClickStat>(MaxBatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                buffer.Clear();

                // Drain whatever is immediately available (non-blocking).
                while (buffer.Count < MaxBatchSize && queue.Reader.TryRead(out var stat))
                {
                    buffer.Add(stat);
                }

                // Otherwise wait a short interval for more events before flushing.
                if (buffer.Count < MaxBatchSize)
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    timeoutCts.CancelAfter(FlushInterval);
                    try
                    {
                        await queue.Reader.WaitToReadAsync(timeoutCts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Flush interval elapsed — proceed with what we have.
                    }

                    while (buffer.Count < MaxBatchSize && queue.Reader.TryRead(out var stat))
                    {
                        buffer.Add(stat);
                    }
                }

                if (buffer.Count > 0)
                {
                    await FlushAsync(buffer, stoppingToken).ConfigureAwait(false);
                }

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Click stats batch failed; {Count} events dropped", buffer.Count);
            }
        }

        // Drain remaining events on shutdown (best effort).
        var leftover = new List<ClickStat>();
        while (queue.Reader.TryRead(out var stat))
        {
            leftover.Add(stat);
        }
        if (leftover.Count > 0)
        {
            try
            {
                await FlushAsync(leftover, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to flush click stats on shutdown");
            }
        }

        logger.LogInformation("Click stats processor stopped");
    }

    private async Task FlushAsync(List<ClickStat> batch, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var counts = batch
            .GroupBy(s => s.ShortLinkId)
            .ToDictionary(g => g.Key, g => g.LongCount());

        db.ClickStats.AddRange(batch);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Denormalized per-link counters: single UPDATE per link.
        foreach (var (linkId, count) in counts)
        {
            await db.ShortLinks
                .Where(l => l.Id == linkId)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(l => l.ClickCount, l => l.ClickCount + count)
                        .SetProperty(l => l.LastRedirectAt, DateTimeOffset.UtcNow),
                    ct)
                .ConfigureAwait(false);
        }
    }
}