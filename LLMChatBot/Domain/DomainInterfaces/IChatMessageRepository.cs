using Domain.Entities;

namespace Domain.DomainInterfaces;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessage>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessage>> GetByConversationIdAfterMessageCountAsync(Guid conversationId, int alreadySummarizedCount, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessage>> GetRecentByConversationIdAsync(Guid conversationId, int count, CancellationToken cancellationToken = default);
    Task<int> GetCountByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetConversationIdsWithAtLeastMessageCountAsync(int minMessageCount, CancellationToken cancellationToken = default);
}
