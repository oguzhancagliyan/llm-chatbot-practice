namespace LLMChatBot.API.Configuration;

public sealed class ConversationSummaryOptions
{
    public const string ConfigSectionName = "ConversationSummary";
    public int MinMessagesBeforeSummary { get; init; } = 20;
    public int SummaryStep { get; init; } = 10;
    public int WorkerIntervalSeconds { get; init; } = 60;
}
