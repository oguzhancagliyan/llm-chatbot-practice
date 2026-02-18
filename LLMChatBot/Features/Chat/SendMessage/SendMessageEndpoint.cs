using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Features.Chat.SendMessage;

public static class SendMessageEndpoint
{
    public static IEndpointRouteBuilder MapSendMessageEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/chat/send", HandleAsync)
            .WithName("SendMessage")
            .WithTags("Chat");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        SendMessageCommand command,
        IValidator<SendMessageCommand> validator,
        SendMessageCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
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

        var response = await handler.HandleAsync(command, cancellationToken);
        return Results.Ok(new SendMessageResponse(command.ConversationId, response));
    }

    private sealed record SendMessageResponse(Guid ConversationId, string Response);
}
