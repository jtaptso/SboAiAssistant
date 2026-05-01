using System.Text;
using SapAiAssistant.Application.Interfaces;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Domain.Entities;

namespace SapAiAssistant.Infrastructure.PromptManagement;

public sealed class PromptRenderer : IPromptRenderer
{
    private readonly IPromptRepository _prompts;

    public PromptRenderer(IPromptRepository prompts)
    {
        _prompts = prompts;
    }

    public async Task<string> RenderAsync(
        AssistantMode mode,
        IReadOnlyList<(MessageRole Role, string Content)> history,
        string userMessage,
        string? sapContext = null,
        CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();

        var systemPrompt = await _prompts.GetTemplateAsync("system", cancellationToken);
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            sb.AppendLine(systemPrompt);
            sb.AppendLine();
        }

        var modeTemplate = mode == AssistantMode.Developer
            ? "developer-instructions"
            : "business-user-instructions";

        var modeInstructions = await _prompts.GetTemplateAsync(modeTemplate, cancellationToken);
        if (!string.IsNullOrWhiteSpace(modeInstructions))
        {
            sb.AppendLine(modeInstructions);
            sb.AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(sapContext))
        {
            sb.AppendLine("### SAP Data Context");
            sb.AppendLine(sapContext);
            sb.AppendLine();
        }

        if (history.Count > 0)
        {
            sb.AppendLine("### Conversation History");
            foreach (var (role, content) in history)
            {
                sb.AppendLine($"{role}: {content}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("### User Message");
        sb.AppendLine(userMessage);

        return sb.ToString();
    }
}
