using Domain.Entities;

namespace Domain.DomainInterfaces;

public interface IRagDocumentRepository
{
    Task ReplaceSourceChunksAsync(
        string sourceId,
        IReadOnlyList<RagDocumentChunk> chunks,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<RagDocumentSearchResult>> SearchByEmbeddingAsync(
        double[] queryEmbedding,
        int topK,
        double minScore,
        CancellationToken cancellationToken = default
    );
}

public sealed record RagDocumentSearchResult(
    string SourceId,
    int ChunkIndex,
    string Content,
    double Score
);
