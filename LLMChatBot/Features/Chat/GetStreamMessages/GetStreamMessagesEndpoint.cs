using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Features.Chat.GetStreamMessages;

public static class GetStreamMessagesEndpoint
{
    public static IEndpointRouteBuilder MapGetStreamMessagesEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/chat/stream/{conversationId:guid}", HandleAsync)
            .WithName("GetStreamMessages")
            .WithTags("Chat");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        Guid conversationId,
        Guid? messageId,
        HttpContext httpContext,
        IValidator<GetStreamMessagesQuery> validator,
        GetStreamMessagesQueryHandler queryHandler,
        CancellationToken cancellationToken)
    {
        var query = new GetStreamMessagesQuery(conversationId, messageId);
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).Distinct().ToArray()
                );

            return Results.ValidationProblem(errors);
        }

        httpContext.Response.Headers.ContentType = "text/event-stream";
        httpContext.Response.Headers.CacheControl = "no-cache";
        httpContext.Response.Headers.Connection = "keep-alive";

        await foreach (var streamEvent in queryHandler.HandleAsync(query, cancellationToken))
        {
            if (query.MessageId.HasValue && streamEvent.MessageId != query.MessageId.Value)
            {
                continue;
            }

            await WriteSseEventAsync(httpContext.Response, streamEvent, cancellationToken);

            if (query.MessageId.HasValue && streamEvent.EventType is "completed" or "error")
            {
                break;
            }
        }

        return Results.Empty;
    }

    private static async Task WriteSseEventAsync(
        HttpResponse response,
        Shared.Abstractions.ChatStreamEvent streamEvent,
        CancellationToken cancellationToken)
    {
        await response.WriteAsync($"id: {streamEvent.Id}\n", cancellationToken);
        await response.WriteAsync($"event: {streamEvent.EventType}\n", cancellationToken);

        var lines = streamEvent.Data.Replace("\r\n", "\n").Split('\n');
        foreach (var line in lines)
        {
            await response.WriteAsync($"data: {line}\n", cancellationToken);
        }

        await response.WriteAsync("\n", cancellationToken);
        await response.Body.FlushAsync(cancellationToken);
    }
}
