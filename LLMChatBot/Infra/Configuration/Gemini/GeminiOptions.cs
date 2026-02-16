namespace Infra.Configuration.Gemini;

public class GeminiOptions : IAgentOptions
{
    public required string ApiKey { get; set; }
    public required string ModelName { get; set; }
    public static string ConfigSectionName { get; set; } = "Gemini";
}
