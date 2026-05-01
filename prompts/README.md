# Prompt Templates

This directory contains versioned prompt templates used by the SapAiAssistant to guide LLM behavior across different user modes and scenarios.

## Files

- **system.txt** — Core system prompt establishing the assistant's architecture knowledge and principles
- **business-user-instructions.txt** — Instructions for the business-user assistance mode
- **developer-instructions.txt** — Instructions for the developer C# code generation mode

## Usage

Prompts are loaded and assembled by `Infrastructure.PromptManagement.PromptStore` during application startup. They are composed in the following order:

1. System prompt (system.txt)
2. Mode-specific instructions (business-user-instructions.txt or developer-instructions.txt)
3. Conversation context and memory
4. User input

## Versioning

Prompt templates should be updated carefully, as they materially affect assistant behavior. Changes should be:

1. Documented with a date and reason
2. Tested against the most recent SAP B1 API documentation
3. Verified with a manual smoke test before deployment
4. Rolled back if they degrade response quality

## Future Enhancements

- Database-backed prompt templates for runtime editing
- A/B testing framework for prompt variants
- Prompt performance metrics (latency, user satisfaction, error rate)
- Integration with a prompt template marketplace or versioning system
