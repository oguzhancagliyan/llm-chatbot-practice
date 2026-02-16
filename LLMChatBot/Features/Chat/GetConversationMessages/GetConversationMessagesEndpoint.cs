using Domain.DomainInterfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Features.Chat.GetConversationMessages;

public static class GetConversationMessagesEndpoint
{
    public static IEndpointRouteBuilder MapGetConversationMessagesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/conversations/{conversationId:guid}/messages", HandleAsync)
            .WithName("GetConversationMessages")
            .WithTags("Chat");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        Guid conversationId,
        IChatMessageRepository repository,
        IConversationCache cache,
        CancellationToken cancellationToken)
    {
        var cachedMessages = await cache.GetConversationMessagesAsync(conversationId, cancellationToken);
        if (cachedMessages is not null)
        {
            return Results.Ok(new GetConversationMessagesResponse("redis", cachedMessages));
        }

        var messages = await repository.GetByConversationIdAsync(conversationId, cancellationToken);
        await cache.SetConversationMessagesAsync(
            conversationId,
            messages,
            TimeSpan.FromMinutes(30),
            cancellationToken
        );

        return Results.Ok(new GetConversationMessagesResponse("postgresql", messages));
    }

    private sealed record GetConversationMessagesResponse(
        string Source,
        IReadOnlyList<Domain.Entities.ChatMessage> Messages
    );
}
