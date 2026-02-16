using System.Diagnostics;
using Domain.DomainInterfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Features.Infra.Health;

public static class InfrastructureHealthEndpoint
{
    public static IEndpointRouteBuilder MapInfrastructureHealthEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/infra/health", HandleAsync)
            .WithName("InfrastructureHealth")
            .WithTags("Infrastructure");

        return endpoints;
    }

    private static async Task<IResult> HandleAsync(
        IChatMessageRepository repository,
        IConversationCache cache,
        CancellationToken cancellationToken)
    {
        var dbStopwatch = Stopwatch.StartNew();
        var dbStatus = "up";
        try
        {
            await repository.GetByConversationIdAsync(Guid.Empty, cancellationToken);
        }
        catch
        {
            dbStatus = "down";
        }
        dbStopwatch.Stop();

        var redisStopwatch = Stopwatch.StartNew();
        var redisStatus = "up";
        try
        {
            await cache.GetConversationMessagesAsync(Guid.Empty, cancellationToken);
        }
        catch
        {
            redisStatus = "down";
        }
        redisStopwatch.Stop();

        return Results.Ok(new
        {
            postgresql = dbStatus,
            redis = redisStatus,
            postgresqlLatencyMs = dbStopwatch.Elapsed.TotalMilliseconds,
            redisLatencyMs = redisStopwatch.Elapsed.TotalMilliseconds
        });
    }
}
