using Domain.Entities;
using Infra.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Abstractions;

namespace Infra.LLMAgents.OpenAI;

public class OpenAiChatModelClient(
    [FromKeyedServices(AgentModels.OpenAi)]
    IChatCompletionService chatCompletionService)
    : IChatModelClient
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

        var response = await chatCompletionService.GetChatMessageContentsAsync(chatHistory);
        return response;
    }
}
