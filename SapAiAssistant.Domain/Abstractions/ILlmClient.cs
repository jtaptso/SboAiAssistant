namespace SapAiAssistant.Domain.Abstractions;

public interface ILlmClient
{
    /// <summary>Sends a prompt to the configured LLM and returns the generated text.</summary>
    /// <param name="prompt">The full assembled prompt.</param>
    /// <param name="model">Override the model to use. <c>null</c> uses the server-side default.</param>
    /// <param name="cancellationToken"></param>
    Task<string> GenerateAsync(string prompt, string? model = null, CancellationToken cancellationToken = default);
}
