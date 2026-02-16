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

    public SendMessageCommandHandler(IConversationCache conversationCache, IChatMessageRepository chatMessageRepository,
        IChatModelClient chatModelClient)
    {
        _conversationCache = conversationCache;
        _chatMessageRepository = chatMessageRepository;
        _chatModelClient = chatModelClient;
    }

    public async Task<string> HandleAsync(SendMessageCommand command, CancellationToken cancellationToken = default)
    {
        var history =
            await _conversationCache.GetConversationMessagesAsync(command.ConversationId, cancellationToken) ??
            new List<ChatMessage>();

        var messages = history.ToList();

        var userMessage = new ChatMessage()
        {
            Role = AuthorRole.User,
            Content = command.Message,
            ConversationId = command.ConversationId,
            ModelId = ""
        };

        messages.Add(userMessage);

        var responseMessages = await _chatModelClient.CompleteAsync(messages, cancellationToken);

        var assistantObj = responseMessages.Last();

        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = command.ConversationId,
            Role = assistantObj.Role,
            Content = assistantObj.Content,
            ModelId = assistantObj.ModelId,
        };

        userMessage.ModelId = assistantObj.ModelId;

        messages.Add(assistantMessage);

        await _conversationCache.SetConversationMessagesAsync(command.ConversationId, messages,
            TimeSpan.FromHours(24), cancellationToken);

        //TODO: implement transaction here
        await _chatMessageRepository.AddAsync(userMessage);
        await _chatMessageRepository.AddAsync(assistantMessage);

        return assistantMessage.Content;
    }
}