using Domain.DomainInterfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistence.PostgreSql;

public class ConversationSummaryRepository(ChatBotDbContext dbContext) : IConversationSummaryRepository
{
    public async Task AddAsync(ConversationSummary summary, CancellationToken cancellationToken = default)
    {
        await dbContext.ConversationSummaries.AddAsync(summary, cancellationToken);
    }

    public Task<ConversationSummary?> GetLatestByConversationIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default
    )
    {
        return dbContext.ConversationSummaries
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExistsByConversationIdAndMessageCountAsync(
        Guid conversationId,
        int messageCount,
        CancellationToken cancellationToken = default
    )
    {
        return dbContext.ConversationSummaries
            .AnyAsync(x => x.ConversationId == conversationId && x.MessageCountAtSummary == messageCount, cancellationToken);
    }
}
