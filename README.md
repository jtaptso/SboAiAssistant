# SAP Business One AI Assistant

A clean-architecture .NET 10 AI assistant for SAP Business One, powered by a local [Ollama](https://ollama.com) LLM. Supports two modes — a **business-user** mode for SAP data lookup and Q&A, and a **developer** mode for SAP B1 C# code generation.

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

Pull the default model:

```bash
ollama pull llama3
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
    "TimeoutMinutes": 10
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

**Response**

```json
{
  "sessionId": "...",
  "messageId": "...",
  "assistantMessage": "...",
  "isGroundedBySap": false,
  "mode": "BusinessUser"
}
```

`isGroundedBySap: true` indicates the response was augmented with live data fetched from SAP.

## Assistant Modes

| Mode | Behaviour |
|---|---|
| `BusinessUser` | Business Q&A — resolves business partner, item, sales order, and invoice lookups from SAP and injects the data into the prompt |
| `Developer` | C# code generation — produces SAP B1 SDK / Service Layer code with explanation separated from code blocks |

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
│       └── SapContextBuilder.cs
├── SapAiAssistant.Domain/           # Core domain
│   ├── Entities/
│   └── ValueObjects/
├── SapAiAssistant.Infrastructure/   # Adapters
│   ├── HealthChecks/
│   ├── IntentDetection/
│   ├── LLM/
│   ├── Memory/
│   ├── Migrations/
│   ├── Persistence/
│   ├── PromptManagement/
│   └── SapIntegration/
├── SapAiAssistant.Web/              # Blazor Server UI
│   ├── Components/
│   │   ├── Chat/
│   │   └── Pages/
│   ├── Services/
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
- Redis cache and vector search for semantic retrieval
- Multi-tenancy
- Write-capable SAP commands (currently read-only by design)
