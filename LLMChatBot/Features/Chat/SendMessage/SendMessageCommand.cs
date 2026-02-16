namespace Features.Chat.SendMessage;

public record SendMessageCommand(Guid ConversationId, string Message, Guid? MessageId = null);
