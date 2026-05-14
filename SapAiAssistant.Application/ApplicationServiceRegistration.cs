using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SapAiAssistant.Application.Interfaces;
using SapAiAssistant.Application.Services;

namespace SapAiAssistant.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RagSettings>(configuration.GetSection("Rag"));

        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ISapContextBuilder, SapContextBuilder>();
        services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
        services.AddScoped<IRagContextProvider, RagContextProvider>();
        return services;
    }
}
