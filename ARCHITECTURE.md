# SAP B1 AI Assistant — Architecture & Design Guide

> A beginner-friendly walkthrough of how the project is built, the patterns used, and why each choice was made. Written to help you explain the project confidently in an interview.

---

## What Does This Project Do?

This is a **chat-based AI assistant for SAP Business One (SAP B1)** — an ERP (Enterprise Resource Planning) system used by businesses to manage customers, inventory, invoices, and sales. Instead of navigating through dozens of screens in SAP, a user can type a question like _"What is the status of sales order 4242?"_ and the assistant fetches the real data from SAP and answers in plain English using a local AI model (Ollama).

There is also a **developer mode** where SAP B1 developers can ask the assistant to generate C# code for SAP B1 integrations.

---

## The Five Projects

The solution is split into five separate C# projects. Think of each one as a distinct layer with a specific job:

```
SapAiAssistant.Domain          ← The core business rules. No dependencies.
SapAiAssistant.Application     ← Orchestrates use cases. Depends on Domain only.
SapAiAssistant.Infrastructure  ← All external concerns (database, AI, SAP, files).
SapAiAssistant.Api             ← REST API. The entry point for requests.
SapAiAssistant.Web             ← Blazor chat UI. Talks to the API.
```

The dependency direction is strict and only flows one way:

```
Domain  ←  Application  ←  Infrastructure
                        ←  Api
                        ←  Web
```

**Domain never knows about Infrastructure.** This is the central rule of the entire architecture.

---

## Architecture Pattern: Clean Architecture

### What is Clean Architecture?

Clean Architecture (popularised by Robert C. Martin, also known as Uncle Bob) is a way of structuring code so that **business rules stay isolated from external concerns** like databases, HTTP, and UI frameworks.

The core idea: your application's logic should not care whether the data comes from SQLite, PostgreSQL, or a cloud database. It should not care whether the UI is Blazor, React, or a mobile app. These details are swappable without touching the business rules.

### How it maps to this project

| Layer | Project | Job |
|---|---|---|
| **Domain** | `SapAiAssistant.Domain` | Defines what a `ChatSession`, `ChatMessage`, `SapIntent`, etc. *are*. No external libraries. |
| **Application** | `SapAiAssistant.Application` | Defines *what the system can do*: send a message, detect intent, build SAP context, render a prompt. Talks to the Domain. Declares interfaces (ports) for things it needs but doesn't implement. |
| **Infrastructure** | `SapAiAssistant.Infrastructure` | Implements those interfaces: Ollama AI client, SQLite database, SAP Service Layer HTTP calls, file-based prompt loading. |
| **Api** | `SapAiAssistant.Api` | Minimal API endpoints. Wires everything together (composition root). |
| **Web** | `SapAiAssistant.Web` | Blazor Server UI. Calls the API to send/receive messages. |

---

## Pattern: Dependency Inversion Principle (DIP)

This is the "D" in **SOLID** and the reason the architecture works.

**The problem it solves:** if `Application` directly created an `OllamaClient`, you could never swap in a different AI provider without editing Application code. Worse, you couldn't test Application logic without a real Ollama server running.

**The solution:** Application defines an *interface* (a contract) and Infrastructure provides the *implementation*.

```
// Domain declares what it needs:
public interface ILlmClient
{
    Task<string> GenerateAsync(string prompt, string? model = null, ...);
}

// Infrastructure provides the real implementation:
public sealed class OllamaClient : ILlmClient { ... }

// In tests, a fake (mock) is used instead:
var llm = Substitute.For<ILlmClient>();
llm.GenerateAsync(...).Returns("fake response");
```

The same pattern is applied everywhere:
- `IConversationRepository` → implemented by `SqliteConversationRepository`
- `ISapAssistantGateway` → implemented by `ServiceLayerGateway` (SAP HTTP calls)
- `IIntentDetector` → implemented by `KeywordIntentDetector`
- `IPromptRenderer` → implemented by `PromptRenderer`
- `IEmbeddingClient` → implemented by `OllamaEmbeddingClient`
- `IVectorStore` → implemented by `SqliteVectorStore`

---

## Pattern: Repository Pattern

**What it is:** A repository is a class that hides how data is stored and retrieved. The rest of the application only knows _"give me a session by ID"_ — it doesn't know whether that means a SQL query, a file read, or an HTTP call.

```csharp
// The contract (Application layer):
public interface IConversationRepository
{
    Task<ChatSession?> GetByIdAsync(Guid sessionId, ...);
    Task<IReadOnlyList<ChatSession>> GetAllAsync(...);
    Task SaveAsync(ChatSession session, ...);
}

// The implementation (Infrastructure layer) uses EF Core + SQLite:
public sealed class SqliteConversationRepository : IConversationRepository
{
    public async Task<ChatSession?> GetByIdAsync(Guid id, ...)
        => await _db.ChatSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == id, ...);
}
```

**Why this matters in an interview:** You can say _"the repository pattern decouples persistence from business logic, which lets me test the business logic in isolation and swap databases without touching Application code."_

---

## Pattern: Dependency Injection (DI)

Instead of classes creating their own dependencies with `new`, all dependencies are declared in the constructor and the framework supplies them automatically at runtime. This is called **Inversion of Control**.

In `InfrastructureServiceRegistration.cs`, all concrete implementations are registered:
```csharp
services.AddScoped<IConversationRepository, SqliteConversationRepository>();
services.AddSingleton<IIntentDetector, KeywordIntentDetector>();
services.AddScoped<IPromptRenderer, PromptRenderer>();
```

When the API receives a request and needs a `ChatService`, .NET automatically injects all of `ChatService`'s dependencies (`IConversationRepository`, `ILlmClient`, etc.) without any manual wiring.

**Lifetimes:**
- `AddSingleton` — one instance for the entire app lifetime (e.g. the intent detector, which has no state).
- `AddScoped` — one instance per HTTP request (e.g. the database context, to avoid sharing state across requests).
- `AddTransient` — a new instance every time it's requested.

---

## Pattern: Domain-Driven Design (DDD) Concepts

The Domain layer uses a few concepts from DDD:

### Entities

Objects with a unique identity that persists over time. In this project:

- **`ChatSession`** — a conversation. Has a unique `Id`, a `Mode` (Business User or Developer), a `Title`, and a list of `ChatMessage` objects.
- **`ChatMessage`** — a single turn in a conversation (user or assistant). Belongs to a `ChatSession` via `SessionId`.
- **`Document`** / **`DocumentChunk`** — a file uploaded to the knowledge base, split into searchable pieces.

Entities are pure data containers with `init`-only properties (set once at creation, never mutated externally):
```csharp
public sealed class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SessionId { get; init; }
    public MessageRole Role { get; init; }
    public string Content { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public bool IsGroundedBySap { get; init; }
}
```

### Value Objects

Objects defined by their *values*, not by an identity. Two `SapIntent` objects with the same `Kind` and `Parameters` are equivalent. In this project:

- **`SapIntent`** — what the user is asking about (e.g. `BusinessPartnerLookup` with `CardCode = "C001"`).
- **`ConversationContext`** — a snapshot of everything needed to build one LLM prompt: session ID, mode, user message, history, SAP data, RAG context.
- **`PromptTemplate`** — a named, versioned prompt text loaded from a file.

---

## How a Message Flows Through the System

This is the most important thing to understand and explain. Here is the full journey of a single user message:

```
User types "Show me customer C001"
          │
          ▼
   [SapAiAssistant.Web]  (Blazor UI)
   Sends POST /api/chat/messages
          │
          ▼
   [SapAiAssistant.Api]  (Minimal API endpoint)
   Calls IChatService.SendMessageAsync(request)
          │
          ▼
   [ChatService]  (Application layer — the brain)
   │
   ├── 1. Load or create a ChatSession from IConversationRepository
   │
   ├── 2. Add the user's message to the session
   │
   ├── 3. Load recent conversation history from IConversationMemoryStore
   │      (last 10 messages, so the AI has context for follow-up questions)
   │
   ├── 4. Detect intent via IIntentDetector
   │      → KeywordIntentDetector scans for keywords like "customer", "invoice"
   │      → Returns SapIntent { Kind = BusinessPartnerLookup, Parameters = { CardCode: "C001" } }
   │
   ├── 5. If intent requires SAP data:
   │      ISapContextBuilder.BuildAsync(intent)
   │      → ServiceLayerGateway calls SAP B1 Service Layer REST API
   │      → Returns formatted text block: "[SAP] CardCode: C001, Name: ACME Ltd, ..."
   │
   ├── 6. RAG (Retrieval-Augmented Generation):
   │      IRagContextProvider.GetContextAsync(userMessage)
   │      → OllamaEmbeddingClient converts the question into a vector (a list of numbers)
   │      → SqliteVectorStore searches uploaded documents for similar content
   │      → Returns relevant excerpts from your knowledge base
   │
   ├── 7. Render prompt via IPromptRenderer
   │      → Loads system prompt + mode-specific instructions from files
   │      → Assembles: system prompt + instructions + SAP data + RAG data + history + user message
   │
   ├── 8. Call ILlmClient.GenerateAsync(prompt)
   │      → OllamaClient sends the prompt to Ollama (local AI model)
   │      → Returns the AI's response text
   │
   ├── 9. Save the assistant's message to the session
   │
   └── 10. Persist the session via IConversationRepository.SaveAsync
           → SqliteConversationRepository writes to SQLite database
```

---

## RAG — Retrieval-Augmented Generation

RAG is a technique that gives the AI access to your own documents without retraining the model. Here is how it works in this project:

1. **Ingestion:** A document (e.g. a PDF or text file) is uploaded via the API. It is split into small chunks (e.g. 500 characters each). Each chunk is converted into a **vector embedding** (a list of floating-point numbers that represent the meaning of the text) using Ollama.

2. **Storage:** The chunks and their embeddings are stored in the SQLite database (`DocumentChunk` table).

3. **Retrieval:** When the user asks a question, the question is also converted to an embedding. The system then finds the chunks whose embeddings are most similar to the question's embedding using **cosine similarity** (a mathematical measure of how "close" two vectors are).

4. **Augmentation:** The most relevant chunks are injected into the prompt before the AI answers, so the AI can reference your documents even though it was never trained on them.

**Why this is powerful for an interview:** RAG is one of the most important patterns in modern AI applications. It solves the problem of "the AI doesn't know about my company's specific data." You can confidently say you implemented a full RAG pipeline from scratch.

---

## Persistence: Entity Framework Core + SQLite

**Entity Framework Core (EF Core)** is an ORM (Object-Relational Mapper) — it lets you work with database tables as C# objects instead of writing raw SQL.

**SQLite** is a file-based database — no server needed, ideal for development and small deployments.

Key choices:
- **Migrations** — EF Core generates SQL migration files that track schema changes. On startup, the API automatically runs `MigrateAsync()` to bring the database schema up to date.
- **`DbContext`** (`AppDbContext`) — the gateway to the database, containing `DbSet<ChatSession>`, `DbSet<ChatMessage>`, `DbSet<Document>`, `DbSet<DocumentChunk>`.
- **`IEntityTypeConfiguration`** — EF Core configuration classes that define table structure, relationships, and cascade delete rules in one place, separate from the entity classes.

---

## Testing Strategy

The project has two test projects with 74 tests total:

### Unit Tests (`SapAiAssistant.Tests.Unit`) — 62 tests

Test one class in isolation. All external dependencies (database, AI, SAP) are replaced with **mocks** (fakes) using the **NSubstitute** library. This means tests run in milliseconds and never need a real network connection.

```csharp
// Example: test ChatService without a real database or AI server
var repo = Substitute.For<IConversationRepository>();
var llm = Substitute.For<ILlmClient>();
llm.GenerateAsync(...).Returns("AI response");

var sut = new ChatService(repo, ...);
var result = await sut.SendMessageAsync(request);

result.AssistantMessage.Should().Be("AI response");
```

### Integration Tests (`SapAiAssistant.Tests.Integration`) — 12 tests

Test the real EF Core + SQLite stack end-to-end using an **in-memory SQLite database**. Each test gets a fresh database. These verify that data is actually persisted and retrieved correctly.

**Testing libraries used:**
- **xUnit** — the test framework
- **NSubstitute** — for creating mocks
- **FluentAssertions** — for readable assertions (`result.Should().Be("x")` instead of `Assert.Equal("x", result)`)

---

## Middleware Pipeline

In `SapAiAssistant.Api`, every HTTP request passes through middleware in order:

1. **`GlobalExceptionHandler`** — catches any unhandled exception anywhere in the pipeline and converts it into a standardised RFC 9457 `ProblemDetails` JSON response (instead of a raw 500 error page). Maps exception types to HTTP status codes (`ArgumentException` → 400, `KeyNotFoundException` → 404, etc.).

2. **`CorrelationIdMiddleware`** — assigns a unique ID to each request (from the `X-Correlation-Id` header, or generates one). This ID is attached to all log entries for that request, so you can trace a single request across thousands of log lines.

3. **Health Checks** — the `/health` endpoint reports whether Ollama, the SQLite database, and the SAP connection are all reachable. Essential for production monitoring.

---

## Design Decisions & Trade-offs (For Interview Q&A)

### "Why five separate projects instead of one?"

Separation of concerns. Each project can be compiled, tested, and — in theory — deployed independently. More importantly, enforcing the dependency rules at the project level means the compiler will reject any attempt to import Infrastructure code into Domain. It is a hard architectural boundary.

### "Why interfaces everywhere?"

Testability and swappability. If `ChatService` directly used `OllamaClient`, you could not test it without Ollama running, and you could not swap Ollama for another AI provider without rewriting the service. Interfaces let both scenarios work.

### "Why SQLite instead of a 'real' database?"

SQLite is a deliberate v1 choice. The repository pattern means the entire database can be swapped for SQL Server or PostgreSQL by adding a new `Infrastructure` implementation. No Application or Domain code would change.

### "Why keyword-based intent detection instead of asking the AI?"

Speed and reliability. Asking the AI to detect intent adds one extra LLM round-trip per message (~1–5 seconds). Keywords are instant. The system is designed so `IIntentDetector` can be swapped for an LLM-based version by changing one registration line in `InfrastructureServiceRegistration.cs`.

### "Why `init` properties on entities instead of methods?"

Immutability reduces bugs. An entity whose properties cannot change after construction is easier to reason about and test. If something must change (like `ChatSession.Title` after creation), it is an explicit `set` property, making mutations visible in code review. The `static Create()` factory method pattern was intentionally removed in favour of simple object initializers, keeping entities as pure data containers with no behaviour.

### "Why local AI (Ollama) instead of OpenAI or Azure OpenAI?"

Privacy, cost, and development simplicity. All data stays local — nothing is sent to a cloud API. The `ILlmClient` interface means swapping to OpenAI later is a one-class change.

---

## Key Vocabulary for Your Interview

| Term | Plain English |
|---|---|
| **Clean Architecture** | Structuring code so business rules don't depend on databases or frameworks |
| **Dependency Inversion** | High-level code depends on abstractions (interfaces), not concrete implementations |
| **Dependency Injection** | The framework supplies class dependencies automatically via constructors |
| **Repository Pattern** | Hides database access behind a simple interface |
| **Interface / Port** | A contract: "I need something that can do X" without specifying how |
| **ORM** | Lets you work with database rows as C# objects (Entity Framework Core) |
| **Migration** | A versioned script that updates the database schema |
| **Mocking** | Replacing a real dependency with a fake one in tests |
| **RAG** | Injecting retrieved document chunks into AI prompts so the AI can answer questions about your data |
| **Embedding** | Converting text into a list of numbers that represents its meaning |
| **Cosine Similarity** | A measure of how similar two embeddings (vectors) are |
| **Middleware** | Code that runs on every HTTP request before/after the endpoint handler |
| **Correlation ID** | A unique ID attached to every request for end-to-end tracing in logs |
| **`init` property** | A C# property that can only be set at object creation time |
| **`sealed class`** | A class that cannot be subclassed (signals "this is a final implementation") |
| **Value Object** | An object defined by its values, not a database identity |
| **Entity** | An object with a unique identity that persists over time |

---

## Things to Say Confidently in an Interview

- _"I applied Clean Architecture to enforce a strict dependency rule: business logic never depends on infrastructure details like the database or the AI provider."_
- _"I used the Repository pattern so persistence is hidden behind an interface. Swapping SQLite for SQL Server would be a one-class change."_
- _"I implemented a full RAG pipeline from scratch — document ingestion, chunking, embedding with Ollama, cosine similarity search in SQLite, and prompt injection."_
- _"All external dependencies are abstracted behind interfaces, which lets me test the Application layer with mocks and without any real infrastructure running."_
- _"I chose Dependency Injection to wire the system together, using ASP.NET Core's built-in container. Each class declares what it needs; the framework supplies it."_
- _"I used `init`-only properties on entities to enforce immutability — properties are set once at creation, which eliminates a class of bugs where state changes happen in unexpected places."_
- _"The intent detection layer is keyword-based in v1, but the `IIntentDetector` interface means I can replace it with LLM-based detection by swapping one class — the rest of the system doesn't change."_
