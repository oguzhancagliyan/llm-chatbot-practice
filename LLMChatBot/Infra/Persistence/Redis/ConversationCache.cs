using System.Text.Json;
using Domain.DomainInterfaces;
using Domain.Entities;
using StackExchange.Redis;

namespace Infra.Persistence.Redis;

public class ConversationCache(IConnectionMultiplexer multiplexer) : IConversationCache
{
    private readonly IDatabase _database = multiplexer.GetDatabase();

    public async Task<IReadOnlyList<ChatMessage>?> GetConversationMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = await _database.StringGetAsync(GetKey(conversationId));
        if (!value.HasValue)
        {
            return null;
        }

        return JsonSerializer.Deserialize<IReadOnlyList<ChatMessage>>(value.ToString());
    }

    public async Task SetConversationMessagesAsync(
        Guid conversationId,
        IReadOnlyList<ChatMessage> messages,
        TimeSpan ttl,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonSerializer.Serialize(messages);
        await _database.StringSetAsync(GetKey(conversationId), payload, ttl);
    }

    public async Task InvalidateConversationAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _database.KeyDeleteAsync(GetKey(conversationId));
    }

    private static string GetKey(Guid conversationId) => $"conversation:{conversationId}:messages";
}
