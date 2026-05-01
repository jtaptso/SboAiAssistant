namespace SapAiAssistant.Domain.Abstractions;

public interface ILlmClient
{
    /// <summary>Sends a prompt to the configured LLM and returns the generated text.</summary>
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}
