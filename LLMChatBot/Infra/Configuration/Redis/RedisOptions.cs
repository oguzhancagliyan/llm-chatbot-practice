namespace Infra.Configuration.Redis;

public sealed class RedisOptions
{
    public const string ConfigSectionName = "Redis";
    public int ConversationCacheTtlMinutes { get; init; } = 30;
    public int EmbeddingCacheTtlHours { get; init; } = 24;
}
