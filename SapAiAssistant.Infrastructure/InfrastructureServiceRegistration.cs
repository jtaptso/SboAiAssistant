using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SapAiAssistant.Application.Interfaces;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Infrastructure.Configuration;
using SapAiAssistant.Infrastructure.HealthChecks;
using SapAiAssistant.Infrastructure.IntentDetection;
using SapAiAssistant.Infrastructure.LLM;
using SapAiAssistant.Infrastructure.Memory;
using SapAiAssistant.Infrastructure.Persistence;
using SapAiAssistant.Infrastructure.PromptManagement;
using SapAiAssistant.Infrastructure.SapIntegration;

[assembly: InternalsVisibleTo("SapAiAssistant.Tests.Integration")]

namespace SapAiAssistant.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.Section));
        services.Configure<SapOptions>(configuration.GetSection(SapOptions.Section));
        services.Configure<PromptOptions>(configuration.GetSection(PromptOptions.Section));

        // Ollama LLM client
        services.AddHttpClient<ILlmClient, OllamaClient>((sp, client) =>
        {
            var options = configuration
                .GetSection(OllamaOptions.Section)
                .Get<OllamaOptions>() ?? new OllamaOptions();

            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromMinutes(options.TimeoutMinutes);
        });

        // SQLite persistence
        var dbPath = configuration["Database:Path"] ?? "sapassistant.db";
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite($"Data Source={dbPath}"));
        services.AddScoped<IConversationRepository, SqliteConversationRepository>();

        // Prompt management
        services.AddSingleton<IPromptRepository, FilePromptRepository>();
        services.AddScoped<IPromptRenderer, PromptRenderer>();

        // Memory
        services.AddScoped<IConversationMemoryStore, ConversationMemoryStore>();

        // Intent detection (keyword-based v1; swap for LLM-based in a later phase)
        services.AddSingleton<IIntentDetector, KeywordIntentDetector>();

        // SAP Business One — Service Layer adapter
        // Each scope gets its own HttpClient with a private CookieContainer so
        // concurrent web requests do not share session cookies.
        services.AddHttpClient<ServiceLayerSession>((sp, client) =>
        {
            var opts = configuration
                .GetSection(SapOptions.Section)
                .Get<SapOptions>() ?? new SapOptions();

            if (!string.IsNullOrWhiteSpace(opts.ServiceLayerBaseUrl))
            {
                client.BaseAddress = new Uri(opts.ServiceLayerBaseUrl.TrimEnd('/') + "/");
            }
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer(),
            // Service Layer typically uses self-signed certs in dev/test
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        });

        services.AddScoped<ServiceLayerSession>();
        services.AddScoped<ISapAssistantGateway, ServiceLayerGateway>();

        // In-memory caching
        services.AddMemoryCache();

        return services;
    }

    /// <summary>
    /// Registers the three infrastructure health checks (Ollama, SQLite, SAP).
    /// Call this on the <see cref="IHealthChecksBuilder"/> returned by
    /// <c>services.AddHealthChecks()</c> in the composition root.
    /// </summary>
    public static IHealthChecksBuilder AddInfrastructureHealthChecks(this IHealthChecksBuilder builder)
    {
        builder
            .AddCheck<OllamaHealthCheck>("ollama",  tags: ["llm"])
            .AddCheck<SqliteHealthCheck>("sqlite",  tags: ["db"])
            .AddCheck<SapHealthCheck>("sap",
                failureStatus: HealthStatus.Degraded,
                tags: ["sap"]);

        return builder;
    }
}
