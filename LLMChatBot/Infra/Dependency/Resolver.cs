using Infra.Configuration;
using Infra.Configuration.AgentSelection;
using Infra.Configuration.Gemini;
using Infra.Configuration.OpenAI;
using Infra.Configuration.Redis;
using Infra.LLMAgents.OpenAI;
using Infra.Persistence.PostgreSql;
using Infra.Persistence.Redis;
using Domain.DomainInterfaces;
using Infra.LLMAgents.Gemini;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.EntityFrameworkCore;
using Shared.Abstractions;
using StackExchange.Redis;

namespace Infra.Dependency;

public static class Resolver
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddPostgreSql(services, configuration);
        AddRedis(services, configuration);
        AddOpenAi(services, configuration);
        AddGemini(services, configuration);
        AddChatModelSelection(services, configuration);
        return services;
    }

    private static void AddPostgreSql(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PostgreSQL")
                               ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL is missing");

        services.AddDbContext<ChatBotDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
    }

    private static void AddRedis(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis")
                               ?? throw new InvalidOperationException("ConnectionStrings:Redis is missing");

        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.ConfigSectionName));
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var configurationOptions = ConfigurationOptions.Parse(connectionString);
            configurationOptions.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(configurationOptions);
        });
        services.AddSingleton<IConversationCache, ConversationCache>();
    }

    private static void AddOpenAi(IServiceCollection services, IConfiguration configuration)
    {
        var openAiOptions = configuration
                                .GetSection(OpenAIOptions.ConfigSectionName)
                                .Get<OpenAIOptions>()
                            ?? throw new InvalidOperationException("OpenAI configuration missing");

        services.AddKeyedSingleton(AgentModels.OpenAi, openAiOptions);

        services.AddKeyedSingleton<IChatCompletionService>(AgentModels.OpenAi, (sp, _) =>
        {
            return new OpenAIChatCompletionService(
                modelId: openAiOptions.ModelName,
                apiKey: openAiOptions.ApiKey
            );
        });
        
        services.AddKeyedScoped<IChatModelClient, OpenAiChatModelClient>(AgentModels.OpenAi);
    }

    private static void AddGemini(IServiceCollection services, IConfiguration configuration)
    {
        var geminiOptions = configuration
                                .GetSection(GeminiOptions.ConfigSectionName)
                                .Get<GeminiOptions>()
                            ?? throw new InvalidOperationException("Gemini configuration missing");

        services.AddKeyedSingleton(AgentModels.Google, geminiOptions);

        services.AddKeyedSingleton<IChatCompletionService>(AgentModels.Google, (sp, _) =>
        {
            return new GoogleAIGeminiChatCompletionService(
                modelId: geminiOptions.ModelName,
                apiKey: geminiOptions.ApiKey
            );
        });
        
        services.AddKeyedScoped<IChatModelClient, GeminiChatModelClient>(AgentModels.Google);
    }

    private static void AddChatModelSelection(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AgentSelectionOptions>(configuration.GetSection(AgentSelectionOptions.ConfigSectionName));

        services.AddScoped<IChatModelClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptionsSnapshot<AgentSelectionOptions>>().Value;
            var provider = (options.Provider ?? string.Empty).Trim();

            var key = provider.ToLowerInvariant() switch
            {
                "openai" => AgentModels.OpenAi,
                "gemini" => AgentModels.Google,
                "google" => AgentModels.Google,
                _ => throw new InvalidOperationException(
                    $"Unsupported AgentSelection:Provider '{options.Provider}'. Use 'OpenAI' or 'Gemini'.")
            };

            return sp.GetRequiredKeyedService<IChatModelClient>(key);
        });
    }
    
    //TODO: Implement mistral and other agents
}
