namespace Shared.Abstractions;

public interface IChatStreamBus
{
    Task PublishAsync(
        Guid messageId,
        Guid conversationId,
        string eventType,
        string data,
        CancellationToken cancellationToken = default
    );

    IAsyncEnumerable<ChatStreamEvent> SubscribeAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default
    );
}

public sealed record ChatStreamEvent(
    string Id,
    Guid MessageId,
    Guid ConversationId,
    string EventType,
    string Data,
    DateTimeOffset CreatedAt
);
