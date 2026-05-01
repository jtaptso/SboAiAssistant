using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SapAiAssistant.Application.DTOs;
using SapAiAssistant.Application.Interfaces;
using SapAiAssistant.Application.Services;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.Entities;
using SapAiAssistant.Domain.ValueObjects;
using FluentAssertions;

namespace SapAiAssistant.Tests.Unit.Application;

public sealed class ChatServiceTests
{
    // ── Substitutes ───────────────────────────────────────────────────────

    private readonly IConversationRepository  _repo       = Substitute.For<IConversationRepository>();
    private readonly IConversationMemoryStore _memory     = Substitute.For<IConversationMemoryStore>();
    private readonly IPromptRenderer          _renderer   = Substitute.For<IPromptRenderer>();
    private readonly ILlmClient               _llm        = Substitute.For<ILlmClient>();
    private readonly IIntentDetector          _detector   = Substitute.For<IIntentDetector>();
    private readonly ISapContextBuilder       _sapBuilder = Substitute.For<ISapContextBuilder>();

    private readonly ChatService _sut;

    public ChatServiceTests()
    {
        _renderer
            .RenderAsync(Arg.Any<AssistantMode>(), Arg.Any<IReadOnlyList<(MessageRole, string)>>(),
                Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns("rendered-prompt");

        _llm.GenerateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("llm-response");

        _memory.GetRecentAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _detector.DetectAsync(Arg.Any<string>(), Arg.Any<AssistantMode>(), Arg.Any<CancellationToken>())
            .Returns(SapIntent.General());

        _sut = new ChatService(_repo, _memory, _renderer, _llm, _detector, _sapBuilder,
            NullLogger<ChatService>.Instance);
    }

    // ── New session ───────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_NewSession_CreatesSessionAndReturnsLlmResponse()
    {
        var request = new SendMessageRequest(null, AssistantMode.BusinessUser, "Hello SAP");

        var response = await _sut.SendMessageAsync(request);

        response.AssistantMessage.Should().Be("llm-response");
        response.SessionId.Should().NotBeEmpty();
        await _repo.Received(1).SaveAsync(Arg.Any<ChatSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendMessage_NewSession_IsGroundedFalse_WhenNoSapContext()
    {
        _sapBuilder.BuildAsync(Arg.Any<SapIntent>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var response = await _sut.SendMessageAsync(
            new SendMessageRequest(null, AssistantMode.BusinessUser, "Tell me about SAP"));

        response.IsGroundedBySap.Should().BeFalse();
    }

    // ── Existing session ──────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_ExistingSession_ReusesSession()
    {
        var session = ChatSession.Create(AssistantMode.BusinessUser);
        var sessionId = session.Id;
        _repo.GetByIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(session);

        var response = await _sut.SendMessageAsync(
            new SendMessageRequest(sessionId, AssistantMode.BusinessUser, "Follow-up"));

        response.SessionId.Should().Be(sessionId);
    }

    // ── SAP grounding ─────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_WhenSapContextReturned_MarksGrounded()
    {
        _detector.DetectAsync(Arg.Any<string>(), Arg.Any<AssistantMode>(), Arg.Any<CancellationToken>())
            .Returns(SapIntent.Create(SapIntentKind.BusinessPartnerLookup,
                new Dictionary<string, string> { ["CardCode"] = "C001" }));

        _sapBuilder.BuildAsync(Arg.Any<SapIntent>(), Arg.Any<CancellationToken>())
            .Returns("[SAP] CardCode: C001");

        var response = await _sut.SendMessageAsync(
            new SendMessageRequest(null, AssistantMode.BusinessUser, "Show customer C001"));

        response.IsGroundedBySap.Should().BeTrue();
    }

    [Fact]
    public async Task SendMessage_GeneralIntent_DoesNotCallSapBuilder()
    {
        _detector.DetectAsync(Arg.Any<string>(), Arg.Any<AssistantMode>(), Arg.Any<CancellationToken>())
            .Returns(SapIntent.General());

        await _sut.SendMessageAsync(
            new SendMessageRequest(null, AssistantMode.BusinessUser, "What is a journal entry?"));

        await _sapBuilder.DidNotReceive().BuildAsync(Arg.Any<SapIntent>(), Arg.Any<CancellationToken>());
    }

    // ── Developer mode ────────────────────────────────────────────────────

    [Fact]
    public async Task SendMessage_DeveloperMode_DoesNotCallSapBuilder()
    {
        _detector.DetectAsync(Arg.Any<string>(), Arg.Any<AssistantMode>(), Arg.Any<CancellationToken>())
            .Returns(SapIntent.Create(SapIntentKind.DeveloperCodeGeneration));

        await _sut.SendMessageAsync(
            new SendMessageRequest(null, AssistantMode.Developer, "Generate a business partner add snippet"));

        await _sapBuilder.DidNotReceive().BuildAsync(Arg.Any<SapIntent>(), Arg.Any<CancellationToken>());
    }

    // ── GetConversation ───────────────────────────────────────────────────

    [Fact]
    public async Task GetConversation_ReturnsNull_WhenSessionNotFound()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((ChatSession?)null);

        var result = await _sut.GetConversationAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConversation_ReturnsMappedDetail_WhenFound()
    {
        var session = ChatSession.Create(AssistantMode.BusinessUser, "Test");
        session.AddMessage(ChatMessage.Create(session.Id, MessageRole.User, "Hi"));
        _repo.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var result = await _sut.GetConversationAsync(session.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test");
        result.Messages.Should().HaveCount(1);
    }
}
