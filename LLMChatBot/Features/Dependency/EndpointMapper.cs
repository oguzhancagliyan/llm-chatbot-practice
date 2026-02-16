using Features.Chat.GetConversationMessages;
using Features.Chat.GetStreamMessages;
using Features.Chat.SendMessage;
using Features.Infra.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Features.Dependency;

public static class EndpointMapper
{
    public static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapInfrastructureHealthEndpoint();
        endpoints.MapSendMessageEndpoint();
        endpoints.MapGetStreamMessagesEndpoint();
        endpoints.MapGetConversationMessagesEndpoint();

        return endpoints;
    }
}
