using Microsoft.Extensions.DependencyInjection;
using SapAiAssistant.Application.Interfaces;
using SapAiAssistant.Application.Services;

namespace SapAiAssistant.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ISapContextBuilder, SapContextBuilder>();
        return services;
    }
}
