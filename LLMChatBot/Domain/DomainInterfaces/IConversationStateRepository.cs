using Domain.Entities;

namespace Domain.DomainInterfaces;

public interface IConversationStateRepository
{
    Task<ConversationState?> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default);
    Task UpsertAsync(ConversationState state, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationState>> GetPendingSummaryStatesAsync(int batchSize, CancellationToken cancellationToken = default);
}
