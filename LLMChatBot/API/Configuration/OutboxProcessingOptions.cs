namespace LLMChatBot.API.Configuration;

public sealed class OutboxProcessingOptions
{
    public const string ConfigSectionName = "OutboxProcessing";
    public int WorkerIntervalSeconds { get; init; } = 5;
    public int BatchSize { get; init; } = 50;
}
