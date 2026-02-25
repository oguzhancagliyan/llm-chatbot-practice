namespace Shared.Configuration;

public sealed class RagOptions
{
    public const string ConfigSectionName = "Rag";
    public bool Enabled { get; init; } = true;
    public int TopK { get; init; } = 4;
    public double MinSimilarityScore { get; init; } = 0.2;
    public int RetrievalCacheTtlSeconds { get; init; } = 300;
    public int MaxChunkLength { get; init; } = 1200;
    public int ChunkOverlap { get; init; } = 200;
}
