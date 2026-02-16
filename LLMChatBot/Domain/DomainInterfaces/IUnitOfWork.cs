namespace Domain.DomainInterfaces;

public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default
    );

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
