using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Infra.Persistence.PostgreSql;

public class ChatBotDbContext(DbContextOptions<ChatBotDbContext> options) : DbContext(options)
{
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

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
    }
}
