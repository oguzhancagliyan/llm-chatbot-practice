using Domain.DomainInterfaces;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Abstractions;
using Shared.Configuration;
using System.Text.Json;

namespace Features.Chat.SendMessage;

public class SendMessageCommandHandler
{
    private readonly IConversationCache _conversationCache;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IConversationSummaryRepository _conversationSummaryRepository;
    private readonly IConversationStateRepository _conversationStateRepository;
    private readonly IOutboxEventRepository _outboxEventRepository;
    private readonly IRagDocumentRepository _ragDocumentRepository;
    private readonly IRagRetrievalCache _ragRetrievalCache;
    private readonly IEmbeddingClient _embeddingClient;
    private readonly RagOptions _ragOptions;
    private readonly IChatModelClient _chatModelClient;
    private readonly IUnitOfWork _unitOfWork;

    public SendMessageCommandHandler(
        IConversationCache conversationCache,
        IChatMessageRepository chatMessageRepository,
        IConversationSummaryRepository conversationSummaryRepository,
        IConversationStateRepository conversationStateRepository,
        IOutboxEventRepository outboxEventRepository,
        IRagDocumentRepository ragDocumentRepository,
        IRagRetrievalCache ragRetrievalCache,
        IEmbeddingClient embeddingClient,
        IOptions<RagOptions> ragOptions,
        IChatModelClient chatModelClient,
        IUnitOfWork unitOfWork)
    {
        _conversationCache = conversationCache;
        _chatMessageRepository = chatMessageRepository;
        _conversationSummaryRepository = conversationSummaryRepository;
        _conversationStateRepository = conversationStateRepository;
        _outboxEventRepository = outboxEventRepository;
        _ragDocumentRepository = ragDocumentRepository;
        _ragRetrievalCache = ragRetrievalCache;
        _embeddingClient = embeddingClient;
        _ragOptions = ragOptions.Value;
        _chatModelClient = chatModelClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<SendMessageResult> HandleAsync(SendMessageCommand command, CancellationToken cancellationToken = default)
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

        var citations = new List<ChatCitation>();
        if (_ragOptions.Enabled)
        {
            var queryEmbedding = await _embeddingClient.GenerateEmbeddingAsync(command.Message, cancellationToken);
            var cachedRagResults = await _ragRetrievalCache.GetAsync(
                queryEmbedding,
                _ragOptions.TopK,
                _ragOptions.MinSimilarityScore,
                cancellationToken
            );

            var ragResults = cachedRagResults ?? await _ragDocumentRepository.SearchByEmbeddingAsync(
                queryEmbedding,
                _ragOptions.TopK,
                _ragOptions.MinSimilarityScore,
                cancellationToken
            );

            if (cachedRagResults is null && ragResults.Count > 0)
            {
                await _ragRetrievalCache.SetAsync(
                    queryEmbedding,
                    _ragOptions.TopK,
                    _ragOptions.MinSimilarityScore,
                    ragResults,
                    cancellationToken
                );
            }

            var bestScore = ragResults.Count > 0 ? ragResults.Max(x => x.Score) : double.MinValue;
            if (bestScore >= _ragOptions.MinSimilarityScore && ragResults.Count > 0)
            {
                var contextLines = ragResults
                    .Select((x, i) => $"[{i + 1}] source={x.SourceId}, chunk={x.ChunkIndex}, score={x.Score:F3}\n{x.Content}")
                    .ToList();

                contextMessages.Add(new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    ConversationId = command.ConversationId,
                    Role = AuthorRole.System,
                    Content =
                        "Use the knowledge base context below when relevant. If context conflicts with the conversation, explain uncertainty and stay factual.\n\n"
                        + string.Join("\n\n", contextLines),
                    ModelId = "rag-retriever"
                });

                citations.AddRange(ragResults.Select(x => new ChatCitation(
                    x.SourceId,
                    x.ChunkIndex,
                    x.Score,
                    x.Content[..Math.Min(180, x.Content.Length)]
                )));
            }
        }

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

        return new SendMessageResult(assistantContent, citations);
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

public sealed record SendMessageResult(string Response, IReadOnlyList<ChatCitation> Citations);
public sealed record ChatCitation(string SourceId, int ChunkIndex, double Score, string Snippet);
