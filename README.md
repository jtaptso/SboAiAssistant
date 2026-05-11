# SAP Business One AI Assistant

A clean-architecture .NET 10 AI assistant for SAP Business One, powered by local [Ollama](https://ollama.com) LLMs. Supports two modes — a **business-user** mode for SAP data lookup and Q&A, and a **developer** mode for SAP B1 C# code generation.

**Current capabilities**
- Chat with multiple locally-running Ollama models (switchable per message via a UI dropdown)
- Live SAP Business One data grounding via Service Layer (business partners, items, orders, invoices)
- Persistent conversation history in SQLite
- RAG knowledge base: upload plain-text documents, retrieve relevant chunks at query time, inject into prompt

## Architecture

```
SapAiAssistant.Domain
  └─ Entities, value objects, pure domain abstractions

SapAiAssistant.Application
  └─ Use cases, orchestration, interfaces (IChatService, ILlmClient, …)

SapAiAssistant.Infrastructure
  └─ Ollama client, SQLite/EF Core, prompt files, SAP Service Layer adapter

SapAiAssistant.Api
  └─ ASP.NET Core minimal API — REST endpoints, health checks, middleware

SapAiAssistant.Web
  └─ Blazor Server chat UI
```

Dependency flow: `Domain ← Application ← {Api, Web}`, `Domain ← Infrastructure ← {Api, Web}`

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 10.0 |
| Ollama | latest |
| SAP Business One Service Layer | optional (chat degrades gracefully without it) |

Pull the required models:

```bash
ollama pull llama3
ollama pull gemma4           # second chat model
ollama pull nomic-embed-text # required for RAG document embedding
```

## Getting Started

```bash
git clone <repo>
cd "SBO AI Assistant"

# Run the API
cd SapAiAssistant.Api
dotnet run

# Run the Blazor UI (separate terminal)
cd SapAiAssistant.Web
dotnet run
```

The API auto-applies EF Core migrations on startup. No manual migration step needed.

## Configuration

Edit `SapAiAssistant.Api/appsettings.json` (or use environment variables / user secrets):

```json
{
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "llama3",
    "TimeoutMinutes": 10,
    "AvailableModels": ["llama3", "gemma4:latest"]
  },
  "Embedding": {
    "Model": "nomic-embed-text",
    "BaseUrl": "http://localhost:11434"
  },
  "Rag": {
    "TopK": 3,
    "MinSimilarityScore": 0.65,
    "ChunkSize": 2000,
    "ChunkOverlap": 200
  },
  "Sap": {
    "ServiceLayerBaseUrl": "https://your-sap-server:50000/b1s/v1",
    "CompanyDb": "YOUR_DB",
    "UserName": "manager",
    "Password": "",
    "TimeoutSeconds": 30
  },
  "Prompts": {
    "TemplatesPath": "../prompts"
  },
  "Database": {
    "Path": "sapassistant.db"
  }
}
```

SAP configuration is optional. When `ServiceLayerBaseUrl` is blank the SAP health check is skipped and all responses are generated from the LLM only.

## API Endpoints

| Method | Path | Description |
|---|---|---|
| `POST` | `/api/chat/messages` | Send a message and receive an assistant reply |
| `GET` | `/api/chat/conversations` | List all saved conversations |
| `GET` | `/api/chat/conversations/{id}` | Get a single conversation with messages |
| `GET` | `/api/models` | List available Ollama models |
| `POST` | `/api/documents` | Upload a document for RAG ingestion (`multipart/form-data`) |
| `GET` | `/api/documents` | List all ingested documents |
| `DELETE` | `/api/documents/{id}` | Delete a document and its chunks |
| `GET` | `/health` | Full JSON health report (Ollama, SQLite, SAP) |
| `GET` | `/health/live` | Lightweight liveness probe |
| `GET` | `/health/sap` | SAP Service Layer availability check |

### Send a message

```bash
curl -X POST http://localhost:5062/api/chat/messages \
  -H "Content-Type: application/json" \
  -d '{"userMessage": "What is SAP Business One?", "mode": "BusinessUser"}'
```

**Request body**

| Field | Type | Values |
|---|---|---|
| `userMessage` | `string` | The user's message |
| `mode` | `string` | `BusinessUser` or `Developer` |
| `sessionId` | `guid?` | Omit to start a new conversation |
| `model` | `string?` | Ollama model name, e.g. `gemma4:latest`. Omit to use the server default |

**Response**

```json
{
  "sessionId": "...",
  "messageId": "...",
  "assistantMessage": "...",
  "isGroundedBySap": false,
  "mode": "BusinessUser",
  "model": "llama3"
}
```

`isGroundedBySap: true` indicates the response was augmented with live data fetched from SAP.

## Assistant Modes

| Mode | Behaviour |
|---|---|
| `BusinessUser` | Business Q&A — resolves business partner, item, sales order, and invoice lookups from SAP and injects the data into the prompt |
| `Developer` | C# code generation — produces SAP B1 SDK / Service Layer code with explanation separated from code blocks |

## Model Selection

The UI toolbar shows a **Model** dropdown populated from `GET /api/models`. Select any available Ollama model before sending a message; the choice travels with the request and is echoed in the response.

To add or remove models, edit `appsettings.json`:

```json
"Ollama": {
  "AvailableModels": ["llama3", "gemma4:latest"]
}
```

## RAG — Knowledge Base

Upload plain-text documents (`.txt`) through the **Knowledge Base** panel in the sidebar. The assistant will automatically retrieve the most relevant passages at query time and include them in the prompt.

**How it works:**

1. On upload, the document is split into overlapping text chunks.
2. Each chunk is embedded using `nomic-embed-text` via Ollama.
3. Embeddings and chunk text are stored in SQLite alongside the conversation data.
4. On each chat message, the user's query is embedded and cosine similarity is used to find the top-K most relevant chunks.
5. Matching chunks are injected into the prompt as a `## Knowledge Base` block before the LLM generates a response.

**Configuration** (`appsettings.json`):

| Key | Default | Description |
|---|---|---|
| `Rag:TopK` | `3` | Maximum number of chunks injected per message |
| `Rag:MinSimilarityScore` | `0.65` | Minimum cosine similarity to include a chunk |
| `Rag:ChunkSize` | `2000` | Characters per chunk |
| `Rag:ChunkOverlap` | `200` | Overlapping characters between consecutive chunks |
| `Embedding:Model` | `nomic-embed-text` | Ollama embedding model |

## Prompt Templates

Prompt templates live in the `prompts/` directory at the solution root:

| File | Purpose |
|---|---|
| `system.txt` | Core system instructions injected into every request |
| `business-user-instructions.txt` | Business user mode instructions |
| `developer-instructions.txt` | Developer mode / C# generation instructions |

Edit these files to tune assistant behaviour without recompiling.

## Health Checks

`GET /health` returns a JSON report:

```json
{
  "status": "Healthy",
  "checks": [
    { "name": "ollama",  "status": "Healthy",  "tags": ["llm"] },
    { "name": "sqlite",  "status": "Healthy",  "tags": ["db"]  },
    { "name": "sap",     "status": "Healthy",  "tags": ["sap"] }
  ]
}
```

The SAP check reports `Degraded` (not `Unhealthy`) when the Service Layer is unreachable — chat continues without SAP grounding.

## Running Tests

```bash
dotnet test SapAiAssistant.slnx
```

60 tests total — 48 unit, 12 integration.

| Suite | Coverage |
|---|---|
| Unit | Domain rules, `ChatService` orchestration, `KeywordIntentDetector`, `SapContextBuilder` |
| Integration | SQLite round-trips (`SqliteConversationRepository`), SAP gateway contract tests with a mock HTTP handler |

## Project Structure

```
SBO AI Assistant/
├── prompts/                         # Prompt templates (versioned plain text)
├── SapAiAssistant.Api/              # Minimal API + middleware
│   ├── Middleware/
│   │   ├── CorrelationIdMiddleware.cs
│   │   └── GlobalExceptionHandler.cs
│   └── Program.cs
├── SapAiAssistant.Application/      # Use cases and interfaces
│   ├── DTOs/
│   ├── Interfaces/
│   └── Services/
│       ├── ChatService.cs
│       ├── DocumentIngestionService.cs  # (Phase 9)
│       ├── RagContextProvider.cs        # (Phase 9)
│       └── SapContextBuilder.cs
├── SapAiAssistant.Domain/           # Core domain
│   ├── Abstractions/                # ILlmClient, IEmbeddingClient, IVectorStore …
│   ├── Entities/                    # ChatSession, ChatMessage, Document, DocumentChunk
│   └── ValueObjects/
├── SapAiAssistant.Infrastructure/   # Adapters
│   ├── Configuration/               # OllamaOptions, EmbeddingOptions, RagOptions …
│   ├── HealthChecks/
│   ├── IntentDetection/
│   ├── LLM/                         # OllamaClient, OllamaEmbeddingClient
│   ├── Memory/
│   ├── Migrations/
│   ├── Persistence/                 # AppDbContext, SqliteConversationRepository, SqliteVectorStore
│   ├── PromptManagement/
│   └── SapIntegration/
├── SapAiAssistant.Web/              # Blazor Server UI
│   ├── Components/
│   │   ├── Chat/                    # ChatInput, MessageThread, ConversationList, ModelSelector
│   │   ├── KnowledgeBase/           # DocumentUpload, DocumentList  (Phase 9)
│   │   └── Pages/
│   ├── Services/                    # ChatState, ApiClient, KnowledgeBaseState (Phase 9)
│   └── wwwroot/
├── SapAiAssistant.Tests.Unit/
└── SapAiAssistant.Tests.Integration/
```

## Observability

Every request carries an `X-Correlation-Id` header (generated if not supplied by the caller). All structured log entries within a request scope include the correlation ID. Unhandled exceptions are caught by `GlobalExceptionHandler` and returned as RFC 9457 `ProblemDetails` with the correlation ID embedded.

## Deferred / Future Work

- SAP DI API adapter (Windows-only, parallel namespace ready)
- Token streaming / SignalR for real-time LLM output
- Production authentication
- Redis cache
- PDF document ingestion (via `UglyToad.PdfPig`)
- `sqlite-vec` or Qdrant for large-corpus vector search (>10 000 chunks)
- Multi-tenancy
- Write-capable SAP commands (currently read-only by design)
