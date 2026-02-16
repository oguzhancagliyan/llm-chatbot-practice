namespace Infra.Configuration;

public interface IAgentOptions
{
    string ApiKey { get; set; }
    string ModelName { get; set; }
}
