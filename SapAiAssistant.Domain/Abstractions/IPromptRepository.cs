namespace SapAiAssistant.Domain.Abstractions;

public interface IPromptRepository
{
    /// <summary>Retrieves a prompt template by its logical name.</summary>
    Task<string> GetTemplateAsync(string name, CancellationToken cancellationToken = default);
}
