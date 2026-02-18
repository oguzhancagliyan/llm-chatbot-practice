using Domain.DomainInterfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistence.PostgreSql;

public class ConversationStateRepository(ChatBotDbContext dbContext) : IConversationStateRepository
{
    public Task<ConversationState?> GetByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return dbContext.ConversationStates
            .FirstOrDefaultAsync(x => x.ConversationId == conversationId, cancellationToken);
    }

    public async Task UpsertAsync(ConversationState state, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.ConversationStates
            .FirstOrDefaultAsync(x => x.ConversationId == state.ConversationId, cancellationToken);

        if (existing is null)
        {
            await dbContext.ConversationStates.AddAsync(state, cancellationToken);
            return;
        }

        existing.MessageCount = state.MessageCount;
        existing.LastSummarizedCount = state.LastSummarizedCount;
        existing.NextSummaryAtCount = state.NextSummaryAtCount;
        existing.SummaryPending = state.SummaryPending;
        existing.LastMessageAt = state.LastMessageAt;
        existing.UpdatedAt = state.UpdatedAt;
    }

    public async Task<IReadOnlyList<ConversationState>> GetPendingSummaryStatesAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await dbContext.ConversationStates
            .Where(x => x.SummaryPending)
            .OrderByDescending(x => x.LastMessageAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
