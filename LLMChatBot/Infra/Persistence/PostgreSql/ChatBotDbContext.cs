using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Infra.Persistence.PostgreSql;

public class ChatBotDbContext(DbContextOptions<ChatBotDbContext> options) : DbContext(options)
{
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ConversationSummary> ConversationSummaries => Set<ConversationSummary>();
    public DbSet<ConversationState> ConversationStates => Set<ConversationState>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<RagDocumentChunk> RagDocumentChunks => Set<RagDocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var authorRoleConverter = new ValueConverter<AuthorRole, string>(
            role => role.Label,
            value => new AuthorRole(value)
        );

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Role)
                .HasConversion(authorRoleConverter)
                .HasMaxLength(32)
                .IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.ConversationId);
        });
        
        modelBuilder.Entity<ConversationSummary>(entitiy =>
        {
            entitiy.ToTable("conversation_summaries");
            entitiy.HasKey(x => x.Id);
            entitiy.Property(x => x.Summary).IsRequired();
            entitiy.Property(x => x.MessageCountAtSummary).IsRequired();
            entitiy.Property(x => x.ModelId).HasMaxLength(128).IsRequired();
            entitiy.Property(x => x.CreatedAt).IsRequired();
            entitiy.HasIndex(x => x.ConversationId);
            entitiy.HasIndex(x => new { x.ConversationId, x.MessageCountAtSummary }).IsUnique();
        });

        modelBuilder.Entity<ConversationState>(entity =>
        {
            entity.ToTable("conversation_states");
            entity.HasKey(x => x.ConversationId);
            entity.Property(x => x.MessageCount).IsRequired();
            entity.Property(x => x.LastSummarizedCount).IsRequired();
            entity.Property(x => x.NextSummaryAtCount).IsRequired();
            entity.Property(x => x.SummaryPending).IsRequired();
            entity.Property(x => x.LastMessageAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
            entity.HasIndex(x => x.SummaryPending);
            entity.HasIndex(x => x.LastMessageAt);
        });

        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("outbox_events");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.Attempts).IsRequired();
            entity.Property(x => x.LastError).HasMaxLength(2000);
            entity.HasIndex(x => x.ProcessedAt);
            entity.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<RagDocumentChunk>(entity =>
        {
            entity.ToTable("rag_document_chunks");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SourceId).HasMaxLength(256).IsRequired();
            entity.Property(x => x.ChunkIndex).IsRequired();
            entity.Property(x => x.Content).IsRequired();
            entity.Property(x => x.Embedding).HasColumnType("double precision[]").IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.SourceId);
            entity.HasIndex(x => new { x.SourceId, x.ChunkIndex }).IsUnique();
        });
    }
}
