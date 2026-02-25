namespace Domain.Entities;

public class RagDocumentChunk
{
    public Guid Id { get; set; }
    public required string SourceId { get; set; }
    public int ChunkIndex { get; set; }
    public required string Content { get; set; }
    public required double[] Embedding { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
