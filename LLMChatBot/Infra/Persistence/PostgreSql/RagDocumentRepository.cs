using Domain.DomainInterfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistence.PostgreSql;

public class RagDocumentRepository(ChatBotDbContext dbContext) : IRagDocumentRepository
{
    public async Task ReplaceSourceChunksAsync(
        string sourceId,
        IReadOnlyList<RagDocumentChunk> chunks,
        CancellationToken cancellationToken = default
    )
    {
        var existing = await dbContext.RagDocumentChunks
            .Where(x => x.SourceId == sourceId)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            dbContext.RagDocumentChunks.RemoveRange(existing);
        }

        if (chunks.Count > 0)
        {
            await dbContext.RagDocumentChunks.AddRangeAsync(chunks, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<RagDocumentSearchResult>> SearchByEmbeddingAsync(
        double[] queryEmbedding,
        int topK,
        double minScore,
        CancellationToken cancellationToken = default
    )
    {
        var chunks = await dbContext.RagDocumentChunks
            .AsNoTracking()
            .Select(x => new { x.SourceId, x.ChunkIndex, x.Content, x.Embedding })
            .ToListAsync(cancellationToken);

        var scored = chunks
            .Select(x => new RagDocumentSearchResult(
                x.SourceId,
                x.ChunkIndex,
                x.Content,
                CosineSimilarity(queryEmbedding, x.Embedding)
            ))
            .Where(x => x.Score >= minScore)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();

        return scored;
    }

    private static double CosineSimilarity(double[] a, double[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
        {
            return -1;
        }

        double dot = 0;
        double normA = 0;
        double normB = 0;

        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
        {
            return -1;
        }

        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }
}
