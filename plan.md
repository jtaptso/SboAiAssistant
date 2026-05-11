# SAP B1 AI Assistant Plan

## Overview

Build a new clean-architecture .NET 10 solution in this folder with five projects:

- `SapAiAssistant.Api`
- `SapAiAssistant.Application`
- `SapAiAssistant.Domain`
- `SapAiAssistant.Infrastructure`
- `SapAiAssistant.Web`

The initial architecture will use Ollama as the first LLM provider, SQLite plus in-memory cache for v1, and SAP Business One Service Layer as the primary integration path. DI API stays behind the same boundary as a later Windows-only adapter. The assistant will also support a developer-oriented mode for SAP Business One C# code generation.

## Solution Structure

```text
SapAiAssistant.sln
|
+-- SapAiAssistant.Api
+-- SapAiAssistant.Application
+-- SapAiAssistant.Domain
+-- SapAiAssistant.Infrastructure
+-- SapAiAssistant.Web
```

## Architecture Direction

- `Domain` contains entities, value objects, and pure business abstractions.
- `Application` contains use cases, orchestration, validators, and ports.
- `Infrastructure` contains Ollama, SQLite, cache, prompt, memory, and SAP adapters.
- `Api` exposes REST endpoints and owns one composition root.
- `Web` provides the Blazor chat UI and talks to the API.

Dependency flow:

```text
Domain <- Application <- Api
                     <- Web
Domain <- Infrastructure
Application <- Infrastructure
```

## Implementation Phases

### Phase 1: Scaffold the Solution

1. Create `SapAiAssistant.sln` and the five projects in this folder, all targeting `net10.0`.
2. Configure project references so `Domain` has no inward dependencies, `Application` depends on `Domain`, and `Api` and `Web` compose `Application` and `Infrastructure`.
3. Enable nullable reference types and implicit usings across all projects.
4. Add central DI registration entry points in `Application` and `Infrastructure`.
5. Add shared configuration binding for Ollama, SQLite, cache, and SAP settings.

### Phase 2: Define Core Contracts

1. In `Domain`, model core concepts such as `ChatSession`, `ChatMessage`, `PromptTemplate`, `ConversationContext`, and `SapIntent`.
2. In `Application`, define interfaces and use cases for:
   - `IChatService`
   - `ILlmClient`
   - `IPromptRenderer`
   - `IConversationMemoryStore`
   - `ISapAssistantGateway`
   - `ISemanticCache` or equivalent cache abstraction
3. Add request and response models plus validators for chat submission and history retrieval.
4. Add an assistant mode or intent split so the system can distinguish business-user help from developer-oriented C# generation requests.
5. Implement the first application vertical slice: accept a message, assemble a prompt, load context, call the LLM, persist the interaction, and return the assistant response.

### Phase 3: Local Infrastructure

1. Add an Ollama client behind a typed interface, using the existing request pattern from [../ConsoleApp/Program.cs](../ConsoleApp/Program.cs) as the initial reference.
2. Add SQLite persistence for conversations and messages.
3. Add in-memory caching for prompt results, intent classification, or SAP metadata lookups with clear TTL rules.
4. Add prompt storage using versioned file-backed templates in Infrastructure. Reference the starter templates in the [prompts/](./prompts/) directory:
   - [system.txt](./prompts/system.txt) — core system prompt
   - [business-user-instructions.txt](./prompts/business-user-instructions.txt) — business user mode
   - [developer-instructions.txt](./prompts/developer-instructions.txt) — C# code generation mode
5. Implement conversation memory with a rolling history window and optional summary compaction.
6. Build the `PromptStore` service in Infrastructure.PromptManagement to load and cache versioned prompt templates from the prompts directory at application startup.

### Phase 4: SAP Business One Integration

1. Implement `ISapAssistantGateway` in Infrastructure with Service Layer as the primary v1 adapter.
2. Handle login or session management, request execution, timeout control, error translation, and mapping to assistant-safe DTOs.
3. Keep DI API out of the first runtime path, but preserve a parallel namespace and adapter shape for later Windows-only support.
4. Limit v1 SAP operations to a narrow allow-list of read-focused workflows:
   - business partner lookup
   - item lookup
   - sales order status
   - invoice lookup
   - company metadata
5. Route intents to approved SAP operations in `Application` instead of exposing generic SAP querying.
6. Define a separate allow-list for developer code-generation scenarios so generated C# stays focused on approved SAP B1 application patterns rather than unrestricted scaffolding.

### Phase 5: External Surfaces

1. In `Api`, expose endpoints for:
   - chat submit
   - conversation history
   - health
   - model status
   - SAP connectivity checks
2. In `Web`, build a Blazor chat shell with:
   - conversation list
   - message thread
   - input box
   - loading state
   - error banner
   - status indicators
3. Make room in the UI and API contract for developer-oriented responses that separate prose explanation from generated C# code.
4. Use standard HTTP endpoints first and defer streaming or SignalR unless needed after the first usability pass.

### Phase 6: Observability and Guardrails

1. Add structured logging and correlation IDs per chat request.
2. Add consistent exception mapping across API and application boundaries.
3. Add startup health checks for Ollama, SQLite access, and SAP configuration.
4. Add safe failure behavior when SAP is unavailable so chat still degrades cleanly.

### Phase 7: Verification

1. Unit test `Domain` rules and `Application` orchestration with mocked adapters.
2. Add Infrastructure integration tests for SQLite persistence and Ollama serialization.
3. Add contract-style tests around the SAP gateway using mocked Service Layer responses.
4. Run one local end-to-end smoke path from Blazor to API to Application to Ollama to persistence.

### Phase 8: Model Selector

Allows users to choose which Ollama model handles their chat session.
Initial supported models: `llama3` (existing default) and `gemma4:latest` (new).

#### 8.1 — Configuration (`SapAiAssistant.Infrastructure`)

- Add `AvailableModels` to `OllamaOptions`:
  ```csharp
  public List<string> AvailableModels { get; set; } = ["llama3", "gemma4:latest"];
  ```
- Populate both entries in `appsettings.json` under the existing `"Ollama"` section:
  ```json
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3",
    "TimeoutMinutes": 10,
    "AvailableModels": ["llama3", "gemma4:latest"]
  }
  ```

#### 8.2 — Domain contract (`SapAiAssistant.Domain`)

- Add an optional `model` parameter to `ILlmClient.GenerateAsync`:
  ```csharp
  Task<string> GenerateAsync(string prompt, string? model = null, CancellationToken cancellationToken = default);
  ```
  A `null` value means "use the server-side default from `OllamaOptions.Model`".

#### 8.3 — Infrastructure (`SapAiAssistant.Infrastructure`)

- Update `OllamaClient.GenerateAsync` to accept the new `model` parameter:
  ```csharp
  var modelToUse = model ?? _options.Model;
  var requestBody = new OllamaRequest(modelToUse, prompt, Stream: false);
  ```

#### 8.4 — Application DTOs and service (`SapAiAssistant.Application`)

- Add `string? Model` to `SendMessageRequest`:
  ```csharp
  public sealed record SendMessageRequest(
      Guid? SessionId,
      AssistantMode Mode,
      string UserMessage,
      string? Model       // null → use server default
  );
  ```
- Add `string Model` to `SendMessageResponse` so the UI can show which model answered:
  ```csharp
  public sealed record SendMessageResponse(
      Guid SessionId,
      Guid MessageId,
      string AssistantMessage,
      bool IsGroundedBySap,
      AssistantMode Mode,
      string Model
  );
  ```
- In `ChatService.SendMessageAsync`, forward `request.Model` to `_llmClient.GenerateAsync` and capture the resolved model name for the response.

#### 8.5 — API surface (`SapAiAssistant.Api`)

- Add a new minimal-API endpoint:
  ```
  GET /api/models
  ```
  Returns the `AvailableModels` list directly from `OllamaOptions` so the Web layer never hard-codes model names.
  ```csharp
  app.MapGet("/api/models", (IOptions<OllamaOptions> opts) =>
      Results.Ok(opts.Value.AvailableModels))
  .WithName("GetModels")
  .WithTags("Models");
  ```

#### 8.6 — Web client (`SapAiAssistant.Web`)

- Add `GetModelsAsync()` to `ApiClient`:
  ```csharp
  public async Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken ct = default)
      => await _http.GetFromJsonAsync<List<string>>("/api/models", ct) ?? [];
  ```
- Extend `ChatState`:
  - Add `IReadOnlyList<string> AvailableModels` (populated at startup).
  - Add `string SelectedModel` (defaults to first in list).
  - Add `void SelectModel(string model)` mutator that calls `Notify()`.
  - Add `string? Model` to the `SendMessageRequest` construction inside `SendMessageAsync`.
  - Load models in `LoadConversationsAsync` (or a dedicated `InitAsync`).
- Update `ChatInput.razor` to emit the model name alongside the message text (or hold it in `ChatState` and let the page compose the request).

#### 8.7 — UI component

- Add a `<ModelSelector>` Blazor component or embed a `<select>` directly in the chat toolbar/sidebar:
  - Renders a `<select>` bound to `ChatState.SelectedModel`.
  - Disabled while `ChatState.IsLoading` is `true`.
  - Placed above or next to the chat input bar.
- When the user changes the selected model, call `ChatState.SelectModel(value)`. The next sent message will use the new selection.

#### 8.8 — Tests

- Unit test `OllamaClient` with an explicit model name override and verify the serialized request uses that name.
- Unit test `ChatService` to confirm the resolved model name is included in `SendMessageResponse`.
- Update existing `SendMessageRequest` unit tests to account for the new `Model` property.

## Initial Scope Decisions

- Included in v1: clean architecture scaffold, Ollama-first LLM path, SQLite, in-memory cache, Service Layer-first SAP boundary, Blazor Web chat UI, prompt management, conversation memory abstractions, and SAP B1 developer assistance with C# code generation.
- Deferred from v1: DI API implementation, SignalR or token streaming, production auth, Redis, vector search, multi-tenancy, broad write-capable SAP commands.
- Phase 8 (model selector) is promoted from deferred to active: adds `gemma4:latest` as a second Ollama model, exposes `GET /api/models`, and adds a dropdown in the Blazor UI. No new LLM provider abstraction is needed for this scope — both models run through the existing `OllamaClient`.
- Recommendation: keep SAP operations read-only until the prompt and orchestration flow is stable.

## Practical Recommendations

1. Start with file-backed prompt templates in Infrastructure rather than database-managed prompts.
2. Use rolling conversation memory first; delay embeddings or vector search until the core workflow is proven.
3. Keep SAP access constrained behind task-specific gateway methods so the assistant remains auditable and safe.
4. Keep developer code generation constrained to a documented set of SAP B1 C# patterns so the first release is predictable and testable.

## Success Criteria

1. The solution builds cleanly with all five projects and correct dependency direction.
2. Api and Web start locally with valid configuration binding and health checks.
3. A local chat request reaches Ollama and returns only assistant text.
4. Conversation history persists in SQLite across restarts.
5. SAP integration failures surface as controlled application errors.
6. The Blazor UI supports send, loading, error handling, and history reload.
7. A developer-mode request returns separated explanation and usable C# code for a supported SAP B1 application scenario.