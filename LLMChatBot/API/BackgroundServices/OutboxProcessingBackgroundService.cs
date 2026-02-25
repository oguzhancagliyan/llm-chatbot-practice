using System.Text.Json;
using Domain.DomainInterfaces;
using Domain.Entities;
using LLMChatBot.API.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Abstractions;

namespace LLMChatBot.API.BackgroundServices;

public class OutboxProcessingBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<OutboxProcessingOptions> outboxOptions,
    IOptions<ConversationSummaryOptions> summaryOptions,
    ILogger<OutboxProcessingBackgroundService> logger) : BackgroundService
{
    private readonly OutboxProcessingOptions _outboxOptions = outboxOptions.Value;
    private readonly ConversationSummaryOptions _summaryOptions = summaryOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox processing worker failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_outboxOptions.WorkerIntervalSeconds), stoppingToken);
        }
    }

    private async Task ProcessOutboxBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var events = await outboxRepository.GetUnprocessedBatchAsync(_outboxOptions.BatchSize, cancellationToken);
        foreach (var outboxEvent in events)
        {
            try
            {
                await HandleOutboxEventAsync(scope.ServiceProvider, outboxEvent, cancellationToken);
                await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    await outboxRepository.MarkProcessedAsync(outboxEvent.Id, ct);
                    await unitOfWork.SaveChangesAsync(ct);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await unitOfWork.ExecuteInTransactionAsync(async ct =>
                {
                    await outboxRepository.MarkFailedAsync(outboxEvent.Id, ex.Message, ct);
                    await unitOfWork.SaveChangesAsync(ct);
                }, cancellationToken);
            }
        }
    }

    private async Task HandleOutboxEventAsync(IServiceProvider sp, OutboxEvent outboxEvent,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(outboxEvent.EventType, "ConversationUpdated", StringComparison.Ordinal))
        {
            return;
        }

        var payload = JsonSerializer.Deserialize<ConversationUpdatedOutboxEvent>(outboxEvent.Payload);
        if (payload is null || payload.ConversationId == Guid.Empty)
        {
            return;
        }

        await HandleConversationUpdatedAsync(sp, payload.ConversationId, cancellationToken);
    }

    private async Task HandleConversationUpdatedAsync(IServiceProvider sp, Guid conversationId,
        CancellationToken cancellationToken)
    {
        var stateRepository = sp.GetRequiredService<IConversationStateRepository>();
        var summaryRepository = sp.GetRequiredService<IConversationSummaryRepository>();
        var messageRepository = sp.GetRequiredService<IChatMessageRepository>();
        var chatModelClient = sp.GetRequiredService<IChatModelClient>();
        var unitOfWork = sp.GetRequiredService<IUnitOfWork>();

        var state = await stateRepository.GetByConversationIdAsync(conversationId, cancellationToken);
        if (state is null || !state.SummaryPending)
        {
            return;
        }

        if (state.MessageCount < _summaryOptions.MinMessagesBeforeSummary)
        {
            state.SummaryPending = false;
            state.UpdatedAt = DateTime.UtcNow;
            await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                await stateRepository.UpsertAsync(state, ct);
                await unitOfWork.SaveChangesAsync(ct);
            }, cancellationToken);
            return;
        }

        var latestSummary = await summaryRepository.GetLatestByConversationIdAsync(conversationId, cancellationToken);
        var summarizedCount = latestSummary?.MessageCountAtSummary ?? state.LastSummarizedCount;
        var newMessageCount = state.MessageCount - summarizedCount;
        if (newMessageCount < _summaryOptions.SummaryStep)
        {
            return;
        }

        var newMessages = await messageRepository.GetByConversationIdAfterMessageCountAsync(
            conversationId,
            summarizedCount,
            cancellationToken
        );

        var filteredMessages = newMessages.Where(c => c.Role == AuthorRole.User || c.Role == AuthorRole.Assistant).ToList();

        if (filteredMessages.Count == 0)
        {
            return;
        }

        var summaryPrompt = BuildSummaryPrompt(latestSummary?.Summary, filteredMessages, conversationId);
        var response = await chatModelClient.CompleteAsync(summaryPrompt, cancellationToken);
        var summaryResponse = response.LastOrDefault();
        var summaryText = summaryResponse?.Content?.Trim();
        if (string.IsNullOrWhiteSpace(summaryText))
        {
            return;
        }

        var summary = new ConversationSummary
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Summary = summaryText,
            MessageCountAtSummary = state.MessageCount,
            ModelId = summaryResponse?.ModelId ?? "unknown-model",
            CreatedAt = DateTime.UtcNow
        };

        state.LastSummarizedCount = state.MessageCount;
        state.NextSummaryAtCount = state.MessageCount + _summaryOptions.SummaryStep;
        state.SummaryPending = false;
        state.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await summaryRepository.AddAsync(summary, ct);
            await stateRepository.UpsertAsync(state, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }, cancellationToken);
    }

    private static IReadOnlyList<ChatMessage> BuildSummaryPrompt(
        string? previousSummary,
        IReadOnlyList<ChatMessage> newMessages,
        Guid conversationId)
    {
        var previousSummaryPart = string.IsNullOrWhiteSpace(previousSummary)
            ? "Previous summary: (none)"
            : $"Previous summary:\n{previousSummary}";

        var promptMessages = new List<ChatMessage>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ConversationId = conversationId,
                Role = AuthorRole.System,
                Content =
                    "You are maintaining a long-term conversation summary.\n\n" +
                    "CRITICAL RULES:\n" +
                    "- Only summarize USER and ASSISTANT conversation messages.\n" +
                    "- DO NOT include any knowledge base context (RAG context).\n" +
                    "- DO NOT include system instructions.\n" +
                    "- DO NOT include citation metadata.\n" +
                    "- Ignore any messages that are marked as coming from external knowledge sources.\n\n" +
                    "Your goal:\n" +
                    "Produce a concise, factual summary that preserves:\n" +
                    "- User intent\n" +
                    "- Constraints\n" +
                    "- Decisions\n" +
                    "- Preferences\n" +
                    "- Important facts about the user\n" +
                    "- Unresolved questions\n\n" +
                    "Keep it short and structured.\n" +
                    "Do not invent information.\n" +
                    "Do not repeat raw conversation text.\n\n" +
                    previousSummaryPart,
                ModelId = "summary-worker"
            }
        };

        promptMessages.AddRange(newMessages);
        return promptMessages;
    }

    private sealed record ConversationUpdatedOutboxEvent(Guid ConversationId);
}