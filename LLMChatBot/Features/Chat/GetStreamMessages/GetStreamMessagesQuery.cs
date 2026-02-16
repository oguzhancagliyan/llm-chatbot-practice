namespace Features.Chat.GetStreamMessages;

public record GetStreamMessagesQuery(Guid ConversationId, Guid? MessageId = null);
