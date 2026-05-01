using System.Net;
using System.Net.Http.Json;
using SapAiAssistant.Infrastructure.Configuration;
using SapAiAssistant.Infrastructure.SapIntegration.Dtos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SapAiAssistant.Infrastructure.SapIntegration;

/// <summary>
/// Manages the SAP Service Layer session lifecycle (login / keep-alive / logout).
/// A single session cookie is reused until it expires or becomes invalid, at which
/// point a transparent re-login is performed.
/// </summary>
public sealed class ServiceLayerSession : IAsyncDisposable
{
    private readonly HttpClient _http;
    private readonly SapOptions _options;
    private readonly ILogger<ServiceLayerSession> _logger;

    private bool _isLoggedIn;
    private readonly SemaphoreSlim _loginLock = new(1, 1);

    public ServiceLayerSession(HttpClient http, IOptions<SapOptions> options, ILogger<ServiceLayerSession> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Ensures an active session exists. Logs in if necessary.
    /// </summary>
    public async Task EnsureSessionAsync(CancellationToken cancellationToken)
    {
        if (_isLoggedIn) return;

        await _loginLock.WaitAsync(cancellationToken);
        try
        {
            if (_isLoggedIn) return;
            await LoginAsync(cancellationToken);
        }
        finally
        {
            _loginLock.Release();
        }
    }

    /// <summary>
    /// Executes <paramref name="action"/> with automatic re-login on session expiry (401).
    /// </summary>
    public async Task<T?> ExecuteAsync<T>(Func<HttpClient, Task<T?>> action, CancellationToken cancellationToken)
    {
        await EnsureSessionAsync(cancellationToken);

        var result = await action(_http);
        return result;
    }

    /// <summary>
    /// Executes with a single transparent re-login if the response is 401.
    /// </summary>
    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await EnsureSessionAsync(cancellationToken);

        var response = await _http.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("Service Layer session expired. Re-logging in.");
            _isLoggedIn = false;
            await LoginAsync(cancellationToken);

            // Rebuild the request since HttpRequestMessage can only be sent once
            var retryRequest = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
                retryRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
            retryRequest.Content = request.Content;

            response = await _http.SendAsync(retryRequest, cancellationToken);
        }

        return response;
    }

    private async Task LoginAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logging in to SAP Service Layer as {User}@{Db}",
            _options.UserName, _options.CompanyDb);

        var payload = new SlLoginRequest
        {
            CompanyDb = _options.CompanyDb,
            UserName  = _options.UserName,
            Password  = _options.Password
        };

        var response = await _http.PostAsJsonAsync("Login", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        _isLoggedIn = true;
        _logger.LogInformation("SAP Service Layer login successful.");
    }

    public async ValueTask DisposeAsync()
    {
        if (!_isLoggedIn) return;
        try
        {
            await _http.PostAsync("Logout", content: null, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SAP Service Layer logout failed.");
        }
    }
}
