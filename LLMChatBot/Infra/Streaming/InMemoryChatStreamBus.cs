using System.Collections.Concurrent;
using System.Threading.Channels;
using Shared.Abstractions;

namespace Infra.Streaming;

public class InMemoryChatStreamBus : IChatStreamBus
{
    private readonly ConcurrentDictionary<Guid, Channel<ChatStreamEvent>> _channels = new();
    private readonly ConcurrentDictionary<Guid, long> _sequenceByConversation = new();

    public Task PublishAsync(
        Guid messageId,
        Guid conversationId,
        string eventType,
        string data,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var channel = _channels.GetOrAdd(
            conversationId,
            _ => Channel.CreateUnbounded<ChatStreamEvent>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            })
        );

        var seq = _sequenceByConversation.AddOrUpdate(conversationId, 1, (_, current) => current + 1);
        var streamEvent = new ChatStreamEvent(
            Id: seq.ToString(),
            MessageId: messageId,
            ConversationId: conversationId,
            EventType: eventType,
            Data: data,
            CreatedAt: DateTimeOffset.UtcNow
        );

        channel.Writer.TryWrite(streamEvent);
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<ChatStreamEvent> SubscribeAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default
    )
    {
        var channel = _channels.GetOrAdd(
            conversationId,
            _ => Channel.CreateUnbounded<ChatStreamEvent>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            })
        );

        return channel.Reader.ReadAllAsync(cancellationToken);
    }
}
