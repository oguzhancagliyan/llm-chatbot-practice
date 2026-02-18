namespace Domain.Entities;

public class ConversationSummary
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public required string Summary { get; set; }
    public int MessageCountAtSummary { get; set; }
    public string ModelId { get; set; } = "unknown-model";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
