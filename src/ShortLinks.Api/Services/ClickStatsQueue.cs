using System.Threading.Channels;
using ShortLinks.Api.Domain;

namespace ShortLinks.Api.Services;

/// <summary>
/// In-process async queue that decouples redirect handling from click persistence.
/// The redirect path only enqueues; a background worker drains and batch-writes.
/// For horizontal scaling this should be swapped for a message broker
/// (RabbitMQ/Kafka) so all instances share one queue.
/// </summary>
public sealed class ClickStatsQueue
{
    private readonly Channel<ClickStat> _channel;

    public ClickStatsQueue(int capacity = 10_000)
    {
        _channel = Channel.CreateBounded<ClickStat>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = false,
            SingleWriter = false,
        });
    }

    /// <summary>Best-effort enqueue. Never blocks or throws on the hot path.</summary>
    public bool TryEnqueue(ClickStat stat) => _channel.Writer.TryWrite(stat);

    public ChannelReader<ClickStat> Reader => _channel.Reader;
}