using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SapAiAssistant.Domain.Abstractions;
using SapAiAssistant.Infrastructure.Configuration;

namespace SapAiAssistant.Infrastructure.PromptManagement;

public sealed class FilePromptRepository : IPromptRepository
{
    private readonly PromptOptions _options;
    private readonly ILogger<FilePromptRepository> _logger;

    // Simple in-memory cache to avoid repeated disk reads
    private readonly Dictionary<string, string> _cache = [];

    public FilePromptRepository(IOptions<PromptOptions> options, ILogger<FilePromptRepository> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Resolve relative paths against the current working directory so the
        // repository works regardless of how the host process was launched.
        if (!Path.IsPathRooted(_options.TemplatesPath))
        {
            _options.TemplatesPath = Path.GetFullPath(_options.TemplatesPath);
        }
    }

    public async Task<string> GetTemplateAsync(string name, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(name, out var cached))
            return cached;

        var filePath = Path.Combine(_options.TemplatesPath, $"{name}.txt");

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Prompt template '{Name}' not found at {Path}", name, filePath);
            return string.Empty;
        }

        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        _cache[name] = content;

        _logger.LogDebug("Loaded prompt template '{Name}' ({Length} chars)", name, content.Length);
        return content;
    }
}
