namespace SapAiAssistant.Domain.Entities;

public sealed class ChatSession
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }
    public AssistantMode Mode { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public IReadOnlyList<ChatMessage> Messages => _messages.AsReadOnly();

    private readonly List<ChatMessage> _messages = [];

    private ChatSession() { Title = string.Empty; } // EF Core

    public static ChatSession Create(AssistantMode mode, string? title = null)
    {
        var now = DateTime.UtcNow;
        return new ChatSession
        {
            Id = Guid.NewGuid(),
            Mode = mode,
            Title = title ?? $"Conversation {now:yyyy-MM-dd HH:mm}",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void AddMessage(ChatMessage message)
    {
        _messages.Add(message);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTitle(string title)
    {
        Title = title;
        UpdatedAt = DateTime.UtcNow;
    }
}
