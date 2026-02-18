using Domain.Entities;

namespace Domain.DomainInterfaces;

public interface IConversationSummaryRepository
{
    Task AddAsync(ConversationSummary summary, CancellationToken cancellationToken = default);
    Task<ConversationSummary?> GetLatestByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByConversationIdAndMessageCountAsync(Guid conversationId, int messageCount, CancellationToken cancellationToken = default);
}
