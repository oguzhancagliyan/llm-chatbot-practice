using Domain.DomainInterfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistence.PostgreSql;

public class ChatMessageRepository(ChatBotDbContext dbContext) : IChatMessageRepository
{
    public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        await dbContext.ChatMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetByConversationIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default
    )
    {
        return await dbContext.ChatMessages
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetByConversationIdAfterMessageCountAsync(
        Guid conversationId,
        int alreadySummarizedCount,
        CancellationToken cancellationToken = default
    )
    {
        return await dbContext.ChatMessages
            .Where(x => x.ConversationId == conversationId)
            .OrderBy(x => x.CreatedAt)
            .Skip(alreadySummarizedCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetRecentByConversationIdAsync(
        Guid conversationId,
        int count,
        CancellationToken cancellationToken = default
    )
    {
        return await dbContext.ChatMessages
            .Where(x => x.ConversationId == conversationId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<int> GetCountByConversationIdAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        return dbContext.ChatMessages
            .Where(x => x.ConversationId == conversationId)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetConversationIdsWithAtLeastMessageCountAsync(
        int minMessageCount,
        CancellationToken cancellationToken = default
    )
    {
        return await dbContext.ChatMessages
            .GroupBy(x => x.ConversationId)
            .Where(g => g.Count() >= minMessageCount)
            .Select(g => g.Key)
            .ToListAsync(cancellationToken);
    }
}
