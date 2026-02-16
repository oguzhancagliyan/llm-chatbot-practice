using Microsoft.SemanticKernel.ChatCompletion;

namespace Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public required AuthorRole Role { get; set; }
    public required string Content { get; set; }
    public required string ModelId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}