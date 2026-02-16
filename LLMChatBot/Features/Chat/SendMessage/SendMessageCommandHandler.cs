using Domain.DomainInterfaces;
using Domain.Entities;
using Microsoft.SemanticKernel.ChatCompletion;
using Shared.Abstractions;

namespace Features.Chat.SendMessage;

public class SendMessageCommandHandler
{
    private readonly IConversationCache _conversationCache;
    private readonly IChatMessageRepository _chatMessageRepository;
    private readonly IChatModelClient _chatModelClient;
    private readonly IUnitOfWork _unitOfWork;

    public SendMessageCommandHandler(
        IConversationCache conversationCache,
        IChatMessageRepository chatMessageRepository,
        IChatModelClient chatModelClient,
        IUnitOfWork unitOfWork)
    {
        _conversationCache = conversationCache;
        _chatMessageRepository = chatMessageRepository;
        _chatModelClient = chatModelClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> HandleAsync(SendMessageCommand command, CancellationToken cancellationToken = default)
    {
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

        var responseMessages = await _chatModelClient.CompleteAsync(messages, cancellationToken);

        var assistantObj = responseMessages.Last();

        var assistantRole = assistantObj.Role;
        var assistantContent = assistantObj.Content ?? string.Empty;
        var modelId = assistantObj.ModelId ?? "unknown-model";

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

        return assistantContent;
    }
}
