using Domain.Entities;

namespace Domain.DomainInterfaces;

public interface IConversationCache
{
    Task<IReadOnlyList<ChatMessage>?> GetConversationMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default
    );

    Task SetConversationMessagesAsync(
        Guid conversationId,
        IReadOnlyList<ChatMessage> messages,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    );

    Task InvalidateConversationAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
