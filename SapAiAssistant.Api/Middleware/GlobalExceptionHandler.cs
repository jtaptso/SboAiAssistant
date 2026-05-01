using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SapAiAssistant.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions, logs them with the correlation ID, and returns
/// a RFC 9457 <see cref="ProblemDetails"/> JSON response instead of the default
/// plain-text 500 page.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items[CorrelationIdMiddleware.HeaderName] as string ?? "unknown";

        _logger.LogError(exception,
            "Unhandled exception [{CorrelationId}] {ExceptionType}: {Message}",
            correlationId, exception.GetType().Name, exception.Message);

        var (status, title) = exception switch
        {
            ArgumentException          => (StatusCodes.Status400BadRequest,  "Bad Request"),
            OperationCanceledException => (StatusCodes.Status408RequestTimeout, "Request Timeout"),
            KeyNotFoundException       => (StatusCodes.Status404NotFound,    "Not Found"),
            _                          => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        httpContext.Response.StatusCode  = status;
        httpContext.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title  = title,
            Detail = exception.Message
        };
        problem.Extensions["correlationId"] = correlationId;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
