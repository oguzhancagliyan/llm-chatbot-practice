using Domain.Entities;
using Infra.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Abstractions;

namespace Infra.LLMAgents.Gemini;

public class GeminiChatModelClient(
    [FromKeyedServices(AgentModels.Google)]
    IChatCompletionService chatCompletionService) : IChatModelClient
{
    public async Task<IReadOnlyList<ChatMessageContent>> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ChatHistory chatHistory = new();

        foreach (var msg in messages)
        {
            chatHistory.Add(new ChatMessageContent
            {
                Content = msg.Content,
                Role = msg.Role
            });
        }

        var response = await chatCompletionService.GetChatMessageContentsAsync(chatHistory, cancellationToken: cancellationToken);
        return response;
    }

    public IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        return StreamInternalAsync(messages, cancellationToken);
    }

    private async IAsyncEnumerable<string> StreamInternalAsync(
        IReadOnlyList<ChatMessage> messages,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        ChatHistory chatHistory = new();

        foreach (var msg in messages)
        {
            chatHistory.Add(new ChatMessageContent
            {
                Content = msg.Content,
                Role = msg.Role
            });
        }

        await foreach (var update in chatCompletionService.GetStreamingChatMessageContentsAsync(
                           chatHistory,
                           cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Content))
            {
                yield return update.Content;
            }
        }
    }
}
