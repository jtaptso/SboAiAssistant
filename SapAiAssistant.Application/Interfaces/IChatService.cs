using SapAiAssistant.Application.DTOs;

namespace SapAiAssistant.Application.Interfaces;

public interface IChatService
{
    Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default);
    Task<ConversationDetail?> GetConversationAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(CancellationToken cancellationToken = default);
}
