using SapAiAssistant.Application.DTOs;
using SapAiAssistant.Application.Interfaces;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.Entities;
using SapAiAssistant.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace SapAiAssistant.Application.Services;

public sealed class ChatService : IChatService
{
    private readonly IConversationRepository _conversations;
    private readonly IConversationMemoryStore _memory;
    private readonly IPromptRenderer _promptRenderer;
    private readonly ILlmClient _llmClient;
    private readonly IIntentDetector _intentDetector;
    private readonly ISapContextBuilder _sapContextBuilder;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IConversationRepository conversations,
        IConversationMemoryStore memory,
        IPromptRenderer promptRenderer,
        ILlmClient llmClient,
        IIntentDetector intentDetector,
        ISapContextBuilder sapContextBuilder,
        ILogger<ChatService> logger)
    {
        _conversations = conversations;
        _memory = memory;
        _promptRenderer = promptRenderer;
        _llmClient = llmClient;
        _intentDetector = intentDetector;
        _sapContextBuilder = sapContextBuilder;
        _logger = logger;
    }

    public async Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        // Load or create session
        ChatSession session;
        if (request.SessionId.HasValue)
        {
            session = await _conversations.GetByIdAsync(request.SessionId.Value, cancellationToken)
                      ?? ChatSession.Create(request.Mode);
        }
        else
        {
            session = ChatSession.Create(request.Mode);
        }

        // Persist the user message
        var userMessage = ChatMessage.Create(session.Id, MessageRole.User, request.UserMessage);
        session.AddMessage(userMessage);

        // Load recent history for context window
        var history = await _memory.GetRecentAsync(session.Id, cancellationToken: cancellationToken);
        var historyTuples = history.Select(m => (m.Role, m.Content)).ToList();

        // Detect intent and optionally fetch SAP grounding data
        var intent = await _intentDetector.DetectAsync(request.UserMessage, request.Mode, cancellationToken);
        _logger.LogInformation("Detected intent {Kind} for session {SessionId}", intent.Kind, session.Id);

        string? sapContext = null;
        if (intent.RequiresSapLookup())
        {
            sapContext = await _sapContextBuilder.BuildAsync(intent, cancellationToken);
            if (sapContext is not null)
                _logger.LogInformation("SAP context fetched for intent {Kind}", intent.Kind);
        }

        // Build ConversationContext value object for the prompt assembler
        var context = ConversationContext.Create(
            session.Id,
            request.Mode,
            request.UserMessage,
            historyTuples,
            sapContext);

        var prompt = await _promptRenderer.RenderAsync(
            context.Mode,
            context.History,
            context.UserMessage,
            context.SapDataContext,
            cancellationToken);

        _logger.LogInformation("Sending prompt to LLM for session {SessionId}", session.Id);

        // Call LLM
        var assistantText = await _llmClient.GenerateAsync(prompt, cancellationToken);

        // Persist assistant response — mark as grounded when SAP data was injected
        var isGrounded = sapContext is not null;
        var assistantMessage = ChatMessage.Create(session.Id, MessageRole.Assistant, assistantText, isGrounded);
        session.AddMessage(assistantMessage);

        await _conversations.SaveAsync(session, cancellationToken);

        return new SendMessageResponse(
            session.Id,
            assistantMessage.Id,
            assistantText,
            assistantMessage.IsGroundedBySap,
            session.Mode);
    }

    public async Task<ConversationDetail?> GetConversationAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _conversations.GetByIdAsync(sessionId, cancellationToken);
        if (session is null) return null;

        return new ConversationDetail(
            session.Id,
            session.Title,
            session.Mode,
            session.CreatedAt,
            session.Messages.Select(m => new MessageDto(m.Id, m.Role, m.Content, m.CreatedAt, m.IsGroundedBySap)).ToList());
    }

    public async Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await _conversations.GetAllAsync(cancellationToken);
        return sessions
            .Select(s => new ConversationSummary(s.Id, s.Title, s.Mode, s.CreatedAt, s.UpdatedAt, s.Messages.Count))
            .OrderByDescending(s => s.UpdatedAt)
            .ToList();
    }
}
