using Domain.DomainInterfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistence.PostgreSql;

public class OutboxEventRepository(ChatBotDbContext dbContext) : IOutboxEventRepository
{
    public async Task AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default)
    {
        await dbContext.OutboxEvents.AddAsync(outboxEvent, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxEvent>> GetUnprocessedBatchAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await dbContext.OutboxEvents
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.OutboxEvents
            .FirstOrDefaultAsync(x => x.Id == eventId, cancellationToken);

        if (existing is null)
        {
            return;
        }

        existing.ProcessedAt = DateTime.UtcNow;
        existing.LastError = null;
    }

    public async Task MarkFailedAsync(Guid eventId, string error, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.OutboxEvents
            .FirstOrDefaultAsync(x => x.Id == eventId, cancellationToken);

        if (existing is null)
        {
            return;
        }

        existing.Attempts += 1;
        existing.LastError = error.Length > 2000 ? error[..2000] : error;
    }
}
