namespace Shared.Abstractions;

public interface IEmbeddingClient
{
    Task<double[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
