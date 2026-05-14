namespace SapAiAssistant.Domain.Entities;

public sealed class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SessionId { get; init; }
    public MessageRole Role { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsGroundedBySap { get; init; }
}
