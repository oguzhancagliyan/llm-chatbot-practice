namespace Infra.Configuration.AgentSelection;

public sealed class AgentSelectionOptions
{
    public const string ConfigSectionName = "AgentSelection";
    public string Provider { get; init; } = "OpenAI";
}
