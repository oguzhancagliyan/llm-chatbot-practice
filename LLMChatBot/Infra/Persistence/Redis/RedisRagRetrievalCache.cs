using Domain.DomainInterfaces;
using Infra.Embeddings;
using Microsoft.Extensions.Options;
using Shared.Abstractions;
using Shared.Configuration;
using StackExchange.Redis;
using System.Globalization;
using System.Text.Json;

namespace Infra.Persistence.Redis;

public class RedisRagRetrievalCache(
    IConnectionMultiplexer multiplexer,
    IKnowledgeBaseVersionProvider knowledgeBaseVersionProvider,
    IOptions<RagOptions> options
) : IRagRetrievalCache
{
    private readonly IDatabase _database = multiplexer.GetDatabase();
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(options.Value.RetrievalCacheTtlSeconds);

    public async Task<IReadOnlyList<RagDocumentSearchResult>?> GetAsync(
        double[] embedding,
        int topK,
        double minScore,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = await BuildKeyAsync(embedding, topK, minScore, cancellationToken);
        var value = await _database.StringGetAsync(key);
        if (!value.HasValue)
        {
            return null;
        }

        return JsonSerializer.Deserialize<List<RagDocumentSearchResult>>(value.ToString());
    }

    public async Task SetAsync(
        double[] embedding,
        int topK,
        double minScore,
        IReadOnlyList<RagDocumentSearchResult> results,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var key = await BuildKeyAsync(embedding, topK, minScore, cancellationToken);
        var payload = JsonSerializer.Serialize(results);
        await _database.StringSetAsync(key, payload, _ttl);
    }

    private async Task<string> BuildKeyAsync(
        double[] embedding,
        int topK,
        double minScore,
        CancellationToken cancellationToken
    )
    {
        var kbVersion = await knowledgeBaseVersionProvider.GetCurrentVersionAsync(cancellationToken);
        var hash = EmbeddingHashUtility.ComputeStableHash(embedding);
        var minScorePart = minScore.ToString("G17", CultureInfo.InvariantCulture);
        return $"rag:retrieval:{kbVersion}:{hash}:{topK}:{minScorePart}";
    }
}
