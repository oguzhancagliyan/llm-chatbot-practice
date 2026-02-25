using Infra.Configuration;
using Infra.Configuration.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Embeddings;
using Shared.Abstractions;

namespace Infra.Embeddings;

public class OpenAiEmbeddingClient(
    [FromKeyedServices(AgentModels.OpenAi)]
    OpenAIOptions openAiOptions
) : IEmbeddingClient
{
    private readonly EmbeddingClient _embeddingClient = new(
        model: openAiOptions.EmbeddingModelName,
        apiKey: openAiOptions.ApiKey
    );

    public async Task<double[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        var embedding = await _embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: cancellationToken);
        var values = embedding.Value.ToFloats();
        var result = new double[values.Length];
        var span = values.Span;
        for (var i = 0; i < span.Length; i++)
        {
            result[i] = span[i];
        }

        return result;
    }
}
