using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Features.Rag.IngestDocument;

public static class IngestDocumentEndpoint
{
    public static IEndpointRouteBuilder MapIngestDocumentEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/rag/documents", HandleAsync)
            .WithName("IngestDocument")
            .WithTags("Rag");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        IngestDocumentCommand command,
        IValidator<IngestDocumentCommand> validator,
        IngestDocumentCommandHandler handler,
        CancellationToken cancellationToken
    )
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

        var result = await handler.HandleAsync(command, cancellationToken);
        return Results.Ok(result);
    }
}
