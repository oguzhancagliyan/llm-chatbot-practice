using Domain.DomainInterfaces;
using Domain.Entities;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Abstractions;
using System.Text.Json;

namespace Features.Chat.SendMessage;

public class SendMessageCommandHandler
{
    private readonly IConversationCache _conversationCache;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IConversationSummaryRepository _conversationSummaryRepository;
    private readonly IConversationStateRepository _conversationStateRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;
    private readonly IChatModelClient _chatModelClient;
    private readonly IUnitOfWork _unitOfWork;

    public SendMessageCommandHandler(
        IConversationCache conversationCache,
        IChatMessageRepository chatMessageRepository,
        IConversationSummaryRepository conversationSummaryRepository,
        IConversationStateRepository conversationStateRepository,
        IOutboxEventRepository outboxEventRepository,
        IChatModelClient chatModelClient,
        IUnitOfWork unitOfWork)
    {
        _conversationCache = conversationCache;
        _chatMessageRepository = chatMessageRepository;
        _conversationSummaryRepository = conversationSummaryRepository;
        _conversationStateRepository = conversationStateRepository;
        _outboxEventRepository = outboxEventRepository;
        _chatModelClient = chatModelClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> HandleAsync(SendMessageCommand command, CancellationToken cancellationToken = default)
    {
        var summary = await _conversationSummaryRepository.GetLatestByConversationIdAsync(command.ConversationId, cancellationToken);
        var recentMessages = await _chatMessageRepository.GetRecentByConversationIdAsync(command.ConversationId, 10, cancellationToken);

        var contextMessages = new List<ChatMessage>();
        if (summary is not null)
        {
            contextMessages.Add(new ChatMessage
            {
                Id = Guid.NewGuid(),
                ConversationId = command.ConversationId,
                Role = AuthorRole.System,
                Content = $"Conversation summary:\n{summary.Summary}",
                ModelId = summary.ModelId
            });
        }

        contextMessages.AddRange(recentMessages);

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            Role = AuthorRole.User,
            Content = command.Message,
            ConversationId = command.ConversationId,
            ModelId = "pending"
        };

        contextMessages.Add(userMessage);
        var llmResponse = await _chatModelClient.CompleteAsync(contextMessages, cancellationToken);
        var assistantObj = llmResponse.LastOrDefault();
        var assistantContent = assistantObj?.Content ?? string.Empty;
        var modelId = assistantObj?.ModelId ?? "unknown-model";

        var assistantRole = AuthorRole.Assistant;

        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = command.ConversationId,
            Role = assistantRole,
            Content = assistantContent,
            ModelId = modelId,
        };

        userMessage.ModelId = modelId;

        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _chatMessageRepository.AddAsync(userMessage, ct);
            await _chatMessageRepository.AddAsync(assistantMessage, ct);
            await UpdateConversationStateAndOutboxAsync(command.ConversationId, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);

        var allConversationMessages = await _chatMessageRepository.GetByConversationIdAsync(command.ConversationId, cancellationToken);
        await _conversationCache.SetConversationMessagesAsync(
            command.ConversationId,
            allConversationMessages,
            TimeSpan.FromHours(24),
            cancellationToken
        );

        return assistantContent;
    }

    private async Task UpdateConversationStateAndOutboxAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var state = await _conversationStateRepository.GetByConversationIdAsync(conversationId, cancellationToken)
                    ?? new ConversationState
                    {
                        ConversationId = conversationId,
                        MessageCount = 0,
                        LastSummarizedCount = 0,
                        NextSummaryAtCount = 20,
                        SummaryPending = false,
                        LastMessageAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

        state.MessageCount += 2;
        state.SummaryPending = true;
        state.LastMessageAt = DateTime.UtcNow;
        state.UpdatedAt = DateTime.UtcNow;

        await _conversationStateRepository.UpsertAsync(state, cancellationToken);

        var outboxPayload = JsonSerializer.Serialize(new ConversationUpdatedOutboxEvent(conversationId));
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EventType = "ConversationUpdated",
            Payload = outboxPayload,
            CreatedAt = DateTime.UtcNow,
            Attempts = 0
        };

        await _outboxEventRepository.AddAsync(outboxEvent, cancellationToken);
    }

    private sealed record ConversationUpdatedOutboxEvent(Guid ConversationId);
}
