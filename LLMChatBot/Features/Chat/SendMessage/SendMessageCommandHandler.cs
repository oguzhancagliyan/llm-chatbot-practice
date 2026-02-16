using Domain.DomainInterfaces;
using Domain.Entities;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Abstractions;
using System.Text;

namespace Features.Chat.SendMessage;

public class SendMessageCommandHandler
{
    private readonly IConversationCache _conversationCache;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IChatModelClient _chatModelClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChatStreamBus _chatStreamBus;

    public SendMessageCommandHandler(
        IConversationCache conversationCache,
        IChatMessageRepository chatMessageRepository,
        IChatModelClient chatModelClient,
        IUnitOfWork unitOfWork,
        IChatStreamBus chatStreamBus)
    {
        _conversationCache = conversationCache;
        _chatMessageRepository = chatMessageRepository;
        _chatModelClient = chatModelClient;
        _unitOfWork = unitOfWork;
        _chatStreamBus = chatStreamBus;
    }

    public async Task<string> HandleAsync(SendMessageCommand command, CancellationToken cancellationToken = default)
    {
        var messageId = command.MessageId ?? Guid.NewGuid();

        var history =
            await _conversationCache.GetConversationMessagesAsync(command.ConversationId, cancellationToken) ??
            new List<ChatMessage>();

        var messages = history.ToList();

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            Role = AuthorRole.User,
            Content = command.Message,
            ConversationId = command.ConversationId,
            ModelId = "pending"
        };

        messages.Add(userMessage);

        await _chatStreamBus.PublishAsync(messageId, command.ConversationId, "started", "stream-started", cancellationToken);

        var assistantContentBuilder = new StringBuilder();
        string modelId = "unknown-model";

        try
        {
            await foreach (var chunk in _chatModelClient.StreamAsync(messages, cancellationToken))
            {
                if (string.IsNullOrEmpty(chunk))
                {
                    continue;
                }

                assistantContentBuilder.Append(chunk);
                await _chatStreamBus.PublishAsync(messageId, command.ConversationId, "chunk", chunk, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await _chatStreamBus.PublishAsync(messageId, command.ConversationId, "error", ex.Message, cancellationToken);
            throw;
        }

        if (assistantContentBuilder.Length == 0)
        {
            var fallbackResponse = await _chatModelClient.CompleteAsync(messages, cancellationToken);
            var fallbackMessage = fallbackResponse.LastOrDefault();
            assistantContentBuilder.Append(fallbackMessage?.Content ?? string.Empty);
            modelId = fallbackMessage?.ModelId ?? modelId;
        }

        var assistantRole = AuthorRole.Assistant;
        var assistantContent = assistantContentBuilder.ToString();

        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = command.ConversationId,
            Role = assistantRole,
            Content = assistantContent,
            ModelId = modelId,
        };

        userMessage.ModelId = modelId;

        messages.Add(assistantMessage);

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _chatMessageRepository.AddAsync(userMessage, ct);
            await _chatMessageRepository.AddAsync(assistantMessage, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        await _conversationCache.SetConversationMessagesAsync(
            command.ConversationId,
            messages,
            TimeSpan.FromHours(24),
            cancellationToken
        );

        await _chatStreamBus.PublishAsync(messageId, command.ConversationId, "completed", assistantContent, cancellationToken);

        return assistantContent;
    }
}
