using Features.Chat.GetStreamMessages;
using Features.Chat.SendMessage;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Features.Dependency;

public static class Resolver
{
    public static IServiceCollection AddFeatures(this IServiceCollection services)
    {
        services.AddScoped<SendMessageCommandHandler>();
        services.AddScoped<IValidator<SendMessageCommand>, SendMessageCommandValidator>();
        services.AddScoped<GetStreamMessagesQueryHandler>();
        services.AddScoped<IValidator<GetStreamMessagesQuery>, GetStreamMessagesQueryValidator>();
        return services;
    }
}
