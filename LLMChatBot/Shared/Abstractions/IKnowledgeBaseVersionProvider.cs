namespace Shared.Abstractions;

public interface IKnowledgeBaseVersionProvider
{
    Task<string> GetCurrentVersionAsync(CancellationToken cancellationToken = default);
    Task BumpVersionAsync(CancellationToken cancellationToken = default);
}
