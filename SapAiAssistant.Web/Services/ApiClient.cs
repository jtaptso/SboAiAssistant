using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    // Match the API's serialisation settings so enums are strings ("BusinessUser" etc.)
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    public ApiClient(HttpClient http) => _http = http;

    public async Task<SendMessageResponse?> SendMessageAsync(
        SendMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "/api/chat/messages", request, _jsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SendMessageResponse>(
            _jsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(
        CancellationToken cancellationToken = default)
        => await _http.GetFromJsonAsync<List<ConversationSummary>>(
               "/api/chat/conversations", _jsonOptions, cancellationToken)
           ?? [];

    public async Task<ConversationDetail?> GetConversationAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
        => await _http.GetFromJsonAsync<ConversationDetail>(
               $"/api/chat/conversations/{sessionId}", _jsonOptions, cancellationToken);

    public async Task<IReadOnlyList<string>> GetModelsAsync(
        CancellationToken cancellationToken = default)
        => await _http.GetFromJsonAsync<List<string>>(
               "/api/models", cancellationToken)
           ?? [];

    public async Task<Guid?> UploadDocumentAsync(
        string name,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(name), "name");
        form.Add(new StreamContent(fileStream), "file", fileName);

        var response = await _http.PostAsync("/api/documents", form, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DocumentUploadResponse>(cancellationToken);
        return result?.DocumentId;
    }

    public async Task<IReadOnlyList<DocumentInfo>> GetDocumentsAsync(
        CancellationToken cancellationToken = default)
        => await _http.GetFromJsonAsync<List<DocumentInfo>>(
               "/api/documents", cancellationToken)
           ?? [];

    public async Task DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var response = await _http.DeleteAsync($"/api/documents/{documentId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}

public sealed record DocumentUploadResponse(Guid DocumentId, string Name);
public sealed record DocumentInfo(Guid Id, string Name, int ChunkCount, DateTime UploadedAt);
