namespace Infra.Configuration.OpenAI;

public class OpenAIOptions : IAgentOptions
{
    public required string ApiKey { get; set; }
    public required string ModelName { get; set; }
    public string EmbeddingModelName { get; set; } = "text-embedding-3-small";
    public static string ConfigSectionName { get; set; } = "OpenAI";
}
