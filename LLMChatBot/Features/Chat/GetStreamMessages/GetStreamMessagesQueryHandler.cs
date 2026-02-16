using Shared.Abstractions;

namespace Features.Chat.GetStreamMessages;

public class GetStreamMessagesQueryHandler(IChatStreamBus chatStreamBus)
{
    public IAsyncEnumerable<ChatStreamEvent> HandleAsync(
        GetStreamMessagesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        return chatStreamBus.SubscribeAsync(query.ConversationId, cancellationToken);
    }
}
