using Shared.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace Infra.Embeddings;

public class EmbeddingClientWithCache(
    OpenAiEmbeddingClient innerClient,
    IEmbeddingCache embeddingCache
) : IEmbeddingClient
{
    public async Task<double[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var normalizedText = NormalizeText(text);

        var cached = await embeddingCache.GetAsync(normalizedText, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var embedding = await innerClient.GenerateEmbeddingAsync(text, cancellationToken);
        await embeddingCache.SetAsync(normalizedText, embedding, cancellationToken);
        return embedding;
    }

    public static string NormalizeText(string text)
    {
        return text.Trim().ToLowerInvariant();
    }

    public static string ComputeSha256Hex(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
