using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Application.DTOs;

public sealed record SendMessageRequest(
    Guid? SessionId,
    AssistantMode Mode,
    string UserMessage,
    string? Model = null
);

public sealed record SendMessageResponse(
    Guid SessionId,
    Guid MessageId,
    string AssistantMessage,
    bool IsGroundedBySap,
    bool IsGroundedByRag,
    AssistantMode Mode,
    string Model
);

public sealed record ConversationSummary(
    Guid Id,
    string Title,
    AssistantMode Mode,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int MessageCount
);

public sealed record ConversationDetail(
    Guid Id,
    string Title,
    AssistantMode Mode,
    DateTime CreatedAt,
    IReadOnlyList<MessageDto> Messages
);

public sealed record MessageDto(
    Guid Id,
    MessageRole Role,
    string Content,
    DateTime CreatedAt,
    bool IsGroundedBySap,
    bool IsGroundedByRag = false
);
