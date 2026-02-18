using Features.Dependency;
using Infra.Dependency;
using Infra.Persistence.PostgreSql;
using LLMChatBot.API.BackgroundServices;
using LLMChatBot.API.Configuration;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFeatures();
builder.Services.Configure<ConversationSummaryOptions>(
    builder.Configuration.GetSection(ConversationSummaryOptions.ConfigSectionName)
);
builder.Services.Configure<OutboxProcessingOptions>(
    builder.Configuration.GetSection(OutboxProcessingOptions.ConfigSectionName)
);
builder.Services.AddHostedService<OutboxProcessingBackgroundService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ChatBotDbContext>();
    await EnsureDatabaseReadyAsync(dbContext, app.Logger, CancellationToken.None);
}

app.UseHttpsRedirection();
app.MapFeatureEndpoints();
app.Run();

static async Task EnsureDatabaseReadyAsync(ChatBotDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
{
    const int maxAttempts = 15;
    var delay = TimeSpan.FromSeconds(2);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await dbContext.Database.EnsureCreatedAsync(cancellationToken);
            await EnsureAppSchemaAsync(dbContext, cancellationToken);
            logger.LogInformation("PostgreSQL is reachable and schema is ready.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "PostgreSQL not ready yet (attempt {Attempt}/{MaxAttempts}). Retrying in {DelaySeconds}s.", attempt, maxAttempts, delay.TotalSeconds);
            await Task.Delay(delay, cancellationToken);
        }
    }

    await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    await EnsureAppSchemaAsync(dbContext, cancellationToken);
}

static async Task EnsureAppSchemaAsync(ChatBotDbContext dbContext, CancellationToken cancellationToken)
{
    const string sql = """
                       CREATE TABLE IF NOT EXISTS conversation_summaries (
                           id uuid PRIMARY KEY,
                           conversation_id uuid NOT NULL,
                           summary text NOT NULL,
                           message_count_at_summary integer NOT NULL,
                           model_id character varying(128) NOT NULL,
                           created_at timestamp with time zone NOT NULL
                       );

                       ALTER TABLE conversation_summaries
                           ADD COLUMN IF NOT EXISTS message_count_at_summary integer NOT NULL DEFAULT 0;

                       ALTER TABLE conversation_summaries
                           ADD COLUMN IF NOT EXISTS model_id character varying(128) NOT NULL DEFAULT 'unknown-model';

                       ALTER TABLE conversation_summaries
                           ADD COLUMN IF NOT EXISTS created_at timestamp with time zone NOT NULL DEFAULT NOW();

                       CREATE UNIQUE INDEX IF NOT EXISTS ix_conversation_summaries_conversation_id_message_count_at_summary
                           ON conversation_summaries (conversation_id, message_count_at_summary);

                       CREATE TABLE IF NOT EXISTS conversation_states (
                           conversation_id uuid PRIMARY KEY,
                           message_count integer NOT NULL,
                           last_summarized_count integer NOT NULL,
                           next_summary_at_count integer NOT NULL,
                           summary_pending boolean NOT NULL,
                           last_message_at timestamp with time zone NOT NULL,
                           updated_at timestamp with time zone NOT NULL
                       );

                       CREATE INDEX IF NOT EXISTS ix_conversation_states_summary_pending
                           ON conversation_states (summary_pending);

                       CREATE INDEX IF NOT EXISTS ix_conversation_states_last_message_at
                           ON conversation_states (last_message_at);

                       CREATE TABLE IF NOT EXISTS outbox_events (
                           id uuid PRIMARY KEY,
                           event_type character varying(128) NOT NULL,
                           payload text NOT NULL,
                           created_at timestamp with time zone NOT NULL,
                           processed_at timestamp with time zone NULL,
                           attempts integer NOT NULL DEFAULT 0,
                           last_error character varying(2000) NULL
                       );

                       CREATE INDEX IF NOT EXISTS ix_outbox_events_processed_at
                           ON outbox_events (processed_at);

                       CREATE INDEX IF NOT EXISTS ix_outbox_events_created_at
                           ON outbox_events (created_at);
                       """;

    await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
}
