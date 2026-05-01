using System.Net.Http.Json;
using SapAiAssistant.Application.DTOs;
using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Web.Services;

/// <summary>
/// Typed HTTP client that talks to the SapAiAssistant.Api backend.
/// Registered via AddHttpClient in Program.cs.
/// </summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http) => _http = http;

    public async Task<SendMessageResponse?> SendMessageAsync(
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
        => await _http.PostAsJsonAsync("/api/chat/messages", request, cancellationToken)
               .ContinueWith(t => t.Result.Content
                   .ReadFromJsonAsync<SendMessageResponse>(cancellationToken),
                   TaskContinuationOptions.OnlyOnRanToCompletion)
               .Unwrap();

    public async Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(
        CancellationToken cancellationToken = default)
        => await _http.GetFromJsonAsync<List<ConversationSummary>>(
               "/api/chat/conversations", cancellationToken)
           ?? [];

    public async Task<ConversationDetail?> GetConversationAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
        => await _http.GetFromJsonAsync<ConversationDetail>(
               $"/api/chat/conversations/{sessionId}", cancellationToken);
}
