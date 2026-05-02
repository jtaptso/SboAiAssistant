using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using SapAiAssistant.Api.Middleware;
using SapAiAssistant.Application;
using SapAiAssistant.Application.DTOs;
using SapAiAssistant.Application.Interfaces;
using SapAiAssistant.Infrastructure;
using SapAiAssistant.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Allow enum values to be sent as strings in JSON (e.g. "BusinessUser" instead of 0)
builder.Services.ConfigureHttpJsonOptions(opts =>
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Exception handling & ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health checks — core + infrastructure probes
builder.Services.AddHealthChecks()
    .AddInfrastructureHealthChecks();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// Apply EF Core migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // interactive UI at /scalar/v1
}

// ── Middleware pipeline ────────────────────────────────────────────────────
app.UseExceptionHandler();    // GlobalExceptionHandler
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseCors();
app.UseHttpsRedirection();

// ── Chat endpoints ─────────────────────────────────────────────────────────

app.MapPost("/api/chat/messages", async (
    SendMessageRequest request,
    IChatService chat,
    CancellationToken ct) =>
{
    var response = await chat.SendMessageAsync(request, ct);
    return Results.Ok(response);
})
.WithName("SendMessage")
.WithTags("Chat");

app.MapGet("/api/chat/conversations", async (
    IChatService chat,
    CancellationToken ct) =>
{
    var conversations = await chat.GetConversationsAsync(ct);
    return Results.Ok(conversations);
})
.WithName("GetConversations")
.WithTags("Chat");

app.MapGet("/api/chat/conversations/{id:guid}", async (
    Guid id,
    IChatService chat,
    CancellationToken ct) =>
{
    var conversation = await chat.GetConversationAsync(id, ct);
    return conversation is null ? Results.NotFound() : Results.Ok(conversation);
})
.WithName("GetConversation")
.WithTags("Chat");

// ── Health endpoints ───────────────────────────────────────────────────────

// Detailed JSON health report used by monitoring tooling
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (ctx, report) =>
    {
        ctx.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name        = e.Key,
                status      = e.Value.Status.ToString(),
                description = e.Value.Description,
                durationMs  = e.Value.Duration.TotalMilliseconds,
                tags        = e.Value.Tags
            })
        }, new JsonSerializerOptions { WriteIndented = false });
        await ctx.Response.WriteAsync(result);
    }
});

// Lightweight liveness probe — 200 = up, 503 = down
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false   // no checks; just confirms the process is alive
});

// SAP-specific health check — uses the "sap" tag registered in AddInfrastructureHealthChecks
app.MapHealthChecks("/health/sap", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("sap")
});

app.Run();

