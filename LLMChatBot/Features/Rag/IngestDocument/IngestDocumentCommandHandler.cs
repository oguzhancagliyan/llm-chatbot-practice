using Domain.DomainInterfaces;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Shared.Abstractions;
using Shared.Configuration;

namespace Features.Rag.IngestDocument;

public class IngestDocumentCommandHandler(
    IRagDocumentRepository ragDocumentRepository,
    IEmbeddingClient embeddingClient,
    IKnowledgeBaseVersionProvider knowledgeBaseVersionProvider,
    IUnitOfWork unitOfWork,
    IOptions<RagOptions> ragOptions
)
{
    private readonly RagOptions _ragOptions = ragOptions.Value;

    public async Task<IngestDocumentResult> HandleAsync(
        IngestDocumentCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var chunks = Chunk(command.Content, _ragOptions.MaxChunkLength, _ragOptions.ChunkOverlap);
        var entities = new List<RagDocumentChunk>(chunks.Count);

        for (var i = 0; i < chunks.Count; i++)
        {
            var embedding = await embeddingClient.GenerateEmbeddingAsync(chunks[i], cancellationToken);
            entities.Add(new RagDocumentChunk
            {
                Id = Guid.NewGuid(),
                SourceId = command.SourceId,
                ChunkIndex = i,
                Content = chunks[i],
                Embedding = embedding,
                CreatedAt = DateTime.UtcNow
            });
        }

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await ragDocumentRepository.ReplaceSourceChunksAsync(command.SourceId, entities, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        await knowledgeBaseVersionProvider.BumpVersionAsync(cancellationToken);

        return new IngestDocumentResult(command.SourceId, entities.Count);
    }

    private static List<string> Chunk(string content, int maxChunkLength, int overlap)
    {
        if (content.Length <= maxChunkLength)
        {
            return [content];
        }

        var chunks = new List<string>();
        var step = Math.Max(1, maxChunkLength - overlap);
        for (var start = 0; start < content.Length; start += step)
        {
            var remaining = content.Length - start;
            var length = Math.Min(maxChunkLength, remaining);
            chunks.Add(content.Substring(start, length));

            if (start + length >= content.Length)
            {
                break;
            }
        }

        return chunks;
    }
}

public sealed record IngestDocumentResult(string SourceId, int ChunkCount);
