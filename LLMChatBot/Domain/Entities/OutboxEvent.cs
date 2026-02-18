namespace Domain.Entities;

public class OutboxEvent
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
