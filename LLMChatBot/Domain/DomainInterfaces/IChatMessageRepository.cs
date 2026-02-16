using Domain.Entities;

namespace Domain.DomainInterfaces;

public interface IChatMessageRepository
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ChatMessage>> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
}
