using Features.Chat.SendMessage;
using Features.Rag.IngestDocument;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Features.Dependency;

public static class Resolver
{
    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        services.AddScoped<SendMessageCommandHandler>();
        services.AddScoped<IValidator<SendMessageCommand>, SendMessageCommandValidator>();
        services.AddScoped<IngestDocumentCommandHandler>();
        services.AddScoped<IValidator<IngestDocumentCommand>, IngestDocumentCommandValidator>();
        return services;
    }
}
