using Domain.Entities;
using Microsoft.SemanticKernel;

namespace Shared.Abstractions;

public interface IChatModelClient
{
    Task<IReadOnlyList<ChatMessageContent>> CompleteAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> StreamAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken cancellationToken = default);
}