using Features.Dependency;
using Infra.Dependency;
using Infra.Persistence.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFeatures();

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
    await EnsureDatabaseReadyAsync(dbContext, app.Logger);
}

app.UseHttpsRedirection();
app.MapFeatureEndpoints();
app.Run();

static async Task EnsureDatabaseReadyAsync(ChatBotDbContext dbContext, ILogger logger)
{
    const int maxAttempts = 15;
    var delay = TimeSpan.FromSeconds(2);

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await dbContext.Database.EnsureCreatedAsync();
            logger.LogInformation("PostgreSQL is reachable and schema is ready.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning(ex, "PostgreSQL not ready yet (attempt {Attempt}/{MaxAttempts}). Retrying in {DelaySeconds}s.", attempt, maxAttempts, delay.TotalSeconds);
            await Task.Delay(delay);
        }
    }

    await dbContext.Database.EnsureCreatedAsync();
}
