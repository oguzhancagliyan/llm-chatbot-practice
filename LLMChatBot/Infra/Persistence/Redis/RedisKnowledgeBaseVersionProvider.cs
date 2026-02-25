using Shared.Abstractions;
using StackExchange.Redis;

namespace Infra.Persistence.Redis;

public class RedisKnowledgeBaseVersionProvider(IConnectionMultiplexer multiplexer) : IKnowledgeBaseVersionProvider
{
    private const string VersionKey = "rag:kb:version";
    private readonly IDatabase _database = multiplexer.GetDatabase();

    public async Task<string> GetCurrentVersionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = await _database.StringGetAsync(VersionKey);
        if (!value.HasValue)
        {
            await _database.StringSetAsync(VersionKey, "1");
            return "1";
        }

        return value.ToString();
    }

    public async Task BumpVersionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _database.StringIncrementAsync(VersionKey);
    }
}
