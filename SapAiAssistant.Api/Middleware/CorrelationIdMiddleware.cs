namespace SapAiAssistant.Api.Middleware;

/// <summary>
/// Reads or generates a <c>X-Correlation-Id</c> per request, echoes it back in the
/// response headers, and pushes it into the structured-logging scope so every log
/// entry written during the request automatically carries the correlation ID.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    internal const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;

        // Store on Items so downstream code (e.g. GlobalExceptionHandler) can read it
        context.Items[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            [nameof(correlationId)] = correlationId
        }))
        {
            await _next(context);
        }
    }
}
