namespace Domain.Entities;

public class ConversationState
{
    public Guid ConversationId { get; set; }
    public int MessageCount { get; set; }
    public int LastSummarizedCount { get; set; }
    public int NextSummaryAtCount { get; set; } = 20;
    public bool SummaryPending { get; set; }
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
