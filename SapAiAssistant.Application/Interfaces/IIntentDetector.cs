using SapAiAssistant.Domain.Entities;
using SapAiAssistant.Domain.ValueObjects;

namespace SapAiAssistant.Application.Interfaces;

public interface IIntentDetector
{
    /// <summary>
    /// Analyses the user message within its assistant mode and returns the detected
    /// SAP intent with any extracted parameters (e.g. CardCode, DocEntry).
    /// </summary>
    Task<SapIntent> DetectAsync(
        string userMessage,
        AssistantMode mode,
        CancellationToken cancellationToken = default);
}
