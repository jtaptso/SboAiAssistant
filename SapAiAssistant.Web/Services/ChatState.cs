using SapAiAssistant.Application.DTOs;
using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Web.Services;

/// <summary>
/// Scoped service that holds the in-memory UI state for the chat shell:
/// conversation list, active session messages, loading/error flags.
/// Components subscribe to <see cref="OnChange"/> to trigger re-renders.
/// </summary>
public sealed class ChatState
{
    private readonly ApiClient _api;

    public IReadOnlyList<ConversationSummary> Conversations { get; private set; } = [];
    public IReadOnlyList<MessageDto> Messages { get; private set; } = [];

    public Guid? ActiveSessionId { get; private set; }
    public AssistantMode ActiveMode { get; private set; } = AssistantMode.BusinessUser;
    public bool IsLoading { get; private set; }
    public string? ErrorMessage { get; private set; }

    public event Action? OnChange;

    public ChatState(ApiClient api) => _api = api;

    // ── Conversations ─────────────────────────────────────────────────────

    public async Task LoadConversationsAsync(CancellationToken ct = default)
    {
        try
        {
            Conversations = await _api.GetConversationsAsync(ct);
            Notify();
        }
        catch (Exception ex)
        {
            SetError($"Failed to load conversations: {ex.Message}");
        }
    }

    public async Task SelectConversationAsync(Guid sessionId, CancellationToken ct = default)
    {
        var detail = await _api.GetConversationAsync(sessionId, ct);
        if (detail is null) return;

        ActiveSessionId = detail.Id;
        ActiveMode      = detail.Mode;
        Messages        = detail.Messages;
        ClearError();
        Notify();
    }

    public void StartNewConversation(AssistantMode mode)
    {
        ActiveSessionId = null;
        ActiveMode      = mode;
        Messages        = [];
        ClearError();
        Notify();
    }

    // ── Messaging ─────────────────────────────────────────────────────────

    public async Task SendMessageAsync(string userText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userText)) return;

        SetLoading(true);

        // Optimistically append the user message to the thread
        var optimistic = new MessageDto(
            Guid.NewGuid(), MessageRole.User, userText, DateTime.UtcNow, false);
        Messages = [.. Messages, optimistic];
        Notify();

        try
        {
            var request = new SendMessageRequest(ActiveSessionId, ActiveMode, userText);
            var response = await _api.SendMessageAsync(request, ct);

            if (response is null)
            {
                SetError("No response from the assistant.");
                return;
            }

            // Confirm session and add assistant message
            ActiveSessionId = response.SessionId;

            var assistantMsg = new MessageDto(
                response.MessageId,
                MessageRole.Assistant,
                response.AssistantMessage,
                DateTime.UtcNow,
                response.IsGroundedBySap);

            Messages = [.. Messages, assistantMsg];

            // Refresh the sidebar list so the new conversation appears
            await LoadConversationsAsync(ct);
            ClearError();
        }
        catch (Exception ex)
        {
            SetError($"Failed to send message: {ex.Message}");
        }
        finally
        {
            SetLoading(false);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void SetLoading(bool value) { IsLoading = value; Notify(); }
    private void SetError(string msg)   { ErrorMessage = msg; IsLoading = false; Notify(); }
    private void ClearError()           { ErrorMessage = null; }
    private void Notify()               => OnChange?.Invoke();
}
