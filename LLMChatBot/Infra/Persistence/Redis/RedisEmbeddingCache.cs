using Infra.Configuration.Redis;
using Infra.Embeddings;
using Microsoft.Extensions.Options;
using Shared.Abstractions;
using StackExchange.Redis;
using System.Text.Json;

namespace Infra.Persistence.Redis;

public class RedisEmbeddingCache(
    IConnectionMultiplexer multiplexer,
    IOptions<RedisOptions> options
) : IEmbeddingCache
{
    private readonly IDatabase _database = multiplexer.GetDatabase();
    private readonly TimeSpan _ttl = TimeSpan.FromHours(options.Value.EmbeddingCacheTtlHours);

    public async Task<double[]?> GetAsync(string normalizedText, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = BuildKey(normalizedText);
        var value = await _database.StringGetAsync(key);
        if (!value.HasValue)
        {
            return null;
        }

        var floatEmbedding = JsonSerializer.Deserialize<float[]>(value.ToString());
        if (floatEmbedding is null)
        {
            return null;
        }

        var result = new double[floatEmbedding.Length];
        for (var i = 0; i < floatEmbedding.Length; i++)
        {
            result[i] = floatEmbedding[i];
        }

        return result;
    }

    public async Task SetAsync(string normalizedText, double[] embedding, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = BuildKey(normalizedText);
        var floatEmbedding = new float[embedding.Length];
        for (var i = 0; i < embedding.Length; i++)
        {
            floatEmbedding[i] = (float)embedding[i];
        }

        var payload = JsonSerializer.Serialize(floatEmbedding);
        await _database.StringSetAsync(key, payload, _ttl);
    }

    private static string BuildKey(string normalizedText)
    {
        var hash = EmbeddingClientWithCache.ComputeSha256Hex(normalizedText);
        return $"embedding:{hash}";
    }
}
