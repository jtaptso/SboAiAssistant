namespace SapAiAssistant.Domain.Entities;

public sealed class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public MessageRole Role { get; private set; }
    public string Content { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsGroundedBySap { get; private set; }

    private ChatMessage() { Content = string.Empty; } // EF Core

    public static ChatMessage Create(Guid sessionId, MessageRole role, string content, bool isGroundedBySap = false)
    {
        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = role,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsGroundedBySap = isGroundedBySap
        };
    }
}
