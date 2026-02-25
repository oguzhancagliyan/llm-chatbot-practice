namespace Shared.Abstractions;

public interface IEmbeddingCache
{
    Task<double[]?> GetAsync(string normalizedText, CancellationToken cancellationToken = default);
    Task SetAsync(string normalizedText, double[] embedding, CancellationToken cancellationToken = default);
}
