using Features.Chat.GetConversationMessages;
using Features.Chat.SendMessage;
using Features.Infra.Health;
using Features.Rag.IngestDocument;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Features.Dependency;

public static class EndpointMapper
{
    public static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapInfrastructureHealthEndpoint();
        endpoints.MapSendMessageEndpoint();
        endpoints.MapGetConversationMessagesEndpoint();
        endpoints.MapIngestDocumentEndpoint();

        return endpoints;
    }
}
