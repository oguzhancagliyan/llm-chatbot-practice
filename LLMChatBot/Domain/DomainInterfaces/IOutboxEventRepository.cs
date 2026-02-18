using Domain.Entities;

namespace Domain.DomainInterfaces;

public interface IOutboxEventRepository
{
    Task AddAsync(OutboxEvent outboxEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxEvent>> GetUnprocessedBatchAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid eventId, string error, CancellationToken cancellationToken = default);
}
