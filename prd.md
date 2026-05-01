# SAP B1 AI Assistant PRD

## Product Summary

SAP B1 AI Assistant is a chat-based assistant for SAP Business One users and SAP Business One developers. It helps business users retrieve information, understand ERP data, and interact with approved SAP workflows through natural language, and it helps developers generate C# code for SAP Business One application scenarios. The first release will focus on read-oriented assistance, grounded in SAP Business One data, with a web chat interface, API backend, local LLM support through Ollama, conversation memory, prompt orchestration, and a clean architecture foundation for future growth.

## Problem Statement

SAP Business One users often need fast answers about customers, items, invoices, sales orders, and company information, but accessing that information usually requires navigating multiple screens, relying on power users, or waiting on technical teams. This creates friction for routine questions and reduces operational speed.

The product should reduce that friction by giving users a controlled AI assistant that can:

- answer SAP-related questions in natural language
- retrieve approved SAP Business One data safely
- preserve useful chat context across a conversation
- provide responses quickly enough for practical daily use
- generate SAP Business One-oriented C# code snippets and implementation guidance for developers
- remain auditable and constrained rather than acting as an unrestricted ERP agent

## Product Vision

Deliver a reliable SAP Business One assistant that combines natural-language interaction with controlled ERP access and targeted developer support, starting with safe read-only workflows and structured C# code generation, then expanding later into richer automation once trust, safety, and operational correctness are established.

## Goals

1. Let a user ask a business question in plain language and receive a useful, context-aware answer.
2. Ground assistant responses in approved SAP Business One data through a controlled integration layer.
3. Support multi-turn conversations with short-term memory and prompt orchestration.
4. Provide a simple Blazor web chat UI for internal users.
5. Establish a clean architecture foundation so LLM providers, persistence, cache, and SAP adapters can evolve independently.
6. Help SAP Business One developers generate usable C# examples, integration snippets, and implementation scaffolds for SAP B1 application scenarios.
7. (Deferred — v2) Let the user choose which LLM model to chat with from a dropdown in the chat interface, without requiring any configuration change or restart.

## Non-Goals For V1

1. Executing broad write operations in SAP Business One.
2. Full autonomous agent behavior with unrestricted tool use.
3. Multi-tenant SaaS support.
4. Enterprise authentication and authorization rollout.
5. Vector database, embeddings, or advanced retrieval pipelines.
6. Streaming chat transport or SignalR-based realtime UX.
7. DI API as the primary runtime integration path.
8. Multi-LLM provider selection UI — deferred to v2 (see FR10 below).

## Target Users

### Primary Users

- SAP Business One operational users
- sales and customer service staff
- finance or back-office staff needing quick ERP lookup assistance
- internal support users who need fast answers from SAP B1 data
- SAP Business One developers building or maintaining C# applications, add-ons, or integrations

### Secondary Users

- system administrators validating SAP connectivity and assistant behavior
- developers extending prompts, adapters, and supported workflows
- business analysts shaping prompt templates and allowed intents

## Key User Scenarios

1. A sales user asks for the status of a sales order and gets a concise answer with relevant identifiers and status details.
2. A finance user asks for invoice information for a customer and receives a grounded summary from approved SAP data.
3. A support user asks about a business partner or item and gets a contextual answer without manually navigating SAP screens.
4. A user asks a follow-up question in the same conversation and the assistant uses prior context to avoid repetition.
5. An administrator checks whether Ollama and SAP connectivity are healthy before rolling the assistant out to users.
6. A SAP Business One developer asks for C# code to call Service Layer, structure an integration service, or implement a SAP B1-related workflow and receives a usable code-first answer.

## V1 Product Scope

### Core Capabilities

- web-based chat experience in Blazor
- HTTP API for chat requests and conversation history
- Ollama-based LLM integration using local models
- prompt assembly pipeline with system prompt, SAP assistant instructions, memory, and user input
- short-term conversation memory and persisted chat history
- SQLite persistence for local development and early deployment
- in-memory caching for selected repeated operations
- SAP Business One Service Layer integration behind a controlled application gateway
- developer-assistant mode for SAP B1-oriented C# code generation and implementation guidance

### Supported SAP V1 Intents

- business partner lookup
- item lookup
- sales order status lookup
- invoice lookup
- company metadata or general ERP information lookup

### Supported Developer V1 Intents

- generate C# Service Layer integration examples
- generate SAP B1 application service scaffolds
- generate DTOs, API clients, and repository patterns for approved SAP workflows
- explain SAP B1 integration patterns in code-oriented terms
- generate implementation snippets for error handling, authentication, and request mapping

### Explicit V1 Constraints

- only approved SAP operations are callable by the assistant
- SAP operations are read-only in v1
- assistant responses should degrade gracefully when SAP is unavailable
- LLM output should be shaped by prompt rules and application logic rather than trusted blindly
- generated code should be treated as suggested output and not executed automatically
- code generation should stay constrained to C# and approved SAP B1 application patterns for v1

## Functional Requirements

### FR1: Chat Interaction

1. The system must allow a user to submit a natural-language message from the web UI.
2. The system must return a text response generated by the LLM.
3. The system must support multi-turn conversations tied to a conversation identifier.
4. The system must persist user and assistant messages for later retrieval.

### FR2: Conversation Memory

1. The system must include recent conversation history in prompt assembly.
2. The system should support memory compaction or summarization when the conversation grows.
3. The system must reload prior messages when a user revisits a conversation.

### FR3: SAP Data Access

1. The system must expose SAP access only through approved application workflows.
2. The system must use SAP Business One Service Layer as the primary integration method for v1.
3. The system must map SAP responses into assistant-safe models before they are used in prompts or returned.
4. The system must handle SAP authentication, session reuse, timeouts, and controlled error translation.

### FR4: Prompt Management

1. The system must support structured prompt assembly for system instructions, context, SAP data, and user input.
2. The system must store prompt templates in a maintainable format.
3. The system should support versioning of prompt templates.

### FR5: Developer Assistance And Code Generation

1. The system must support developer-oriented prompts for SAP Business One C# generation.
2. The system must generate readable C# snippets or scaffolds for approved SAP B1 application scenarios.
3. The system should distinguish between business-user assistance and developer assistance in prompt orchestration.
4. The system should structure generated code around known SAP B1 integration patterns such as Service Layer clients, DTOs, repositories, and application services.
5. The system must present generated code as advisory output rather than directly executable automation.

### FR6: Caching

1. The system should cache selected expensive or repeated lookups.
2. The system must support configurable cache expiry policies.
3. The system must avoid serving unsafe stale data for time-sensitive SAP results.

### FR7: Web UI

1. The system must provide a chat interface with a message thread and input area.
2. The system must show loading and error states clearly.
3. The system must let users reopen and review previous conversations.
4. The system should show system status indicators for model and backend availability.

### FR8: API Surface

1. The system must expose endpoints for sending chat messages and retrieving conversation history.
2. The system must expose health endpoints for the API, model availability, and SAP connectivity checks.
3. The system must return controlled, consistent error responses.

### FR9: Observability

1. The system must log chat requests, SAP access attempts, and failure paths with correlation identifiers.
2. The system must support tracing a single conversation across UI, API, application flow, and infrastructure calls.

### FR10: LLM Provider Selection (Deferred — V2)

1. The system must expose a list of available LLM providers / models via a dedicated API endpoint.
2. The chat UI must display a dropdown near the input bar that lists available models and lets the user switch before or during a conversation.
3. The selected model must travel with each chat request so the backend routes to the correct LLM client.
4. The selected model must be persisted per conversation session so reopening a past conversation shows which model was used.
5. Adding a new model must require only a configuration entry — no code change or redeployment.
6. The system must fail gracefully (with a clear UI error) if the selected model is unavailable at request time.

## Non-Functional Requirements

### Performance

1. The system should return simple non-SAP chat responses within an acceptable local-model response window.
2. The system should keep SAP-backed lookups responsive enough for interactive use.
3. The system should avoid unnecessary repeated model or SAP calls through prompt discipline and selective caching.
4. The system should return developer code-generation responses in a format that is readable and practical for copy-and-adapt workflows.

### Reliability

1. The system must fail gracefully if Ollama is unavailable.
2. The system must fail gracefully if SAP Business One is unavailable.
3. The system must preserve chat history even if individual enrichment calls fail.

### Security and Safety

1. The system must not expose unrestricted SAP querying in v1.
2. The system must constrain SAP access through allow-listed operations.
3. The system must avoid leaking raw infrastructure exceptions directly to end users.
4. The system should be structured so authentication and authorization can be added cleanly later.
5. The system should avoid generating unsafe guidance that implies unrestricted ERP access patterns without explicit constraints.

### Maintainability

1. The system must follow clean architecture boundaries.
2. The system must allow Ollama to be replaced by another LLM provider with minimal application-layer change.
3. The system must allow Service Layer and DI API adapters to share a stable application-facing contract.

## User Experience Requirements

1. The chat experience should feel simple and direct, optimized for quick question-and-answer workflows.
2. The UI should clearly indicate when the system is waiting on the model or SAP.
3. The UI should present errors in plain language without exposing technical internals.
4. Responses should be readable and focused rather than dumping raw JSON.
5. The user should be able to understand whether an answer came from model reasoning, SAP data grounding, or both.
6. Developer responses should clearly separate explanation from generated C# code.
7. (V2) The model selector dropdown should sit next to the chat input, be compact enough not to distract, and display the currently active model name at a glance.

## Success Metrics

1. A user can complete a basic SAP lookup scenario end-to-end from the chat UI.
2. The application builds and runs locally with Ollama, SQLite, API, and Web components functioning together.
3. Conversation history persists and reloads correctly.
4. SAP integration errors are controlled and recoverable.
5. Prompt and infrastructure changes can be made without breaking application-layer use cases.
6. A SAP B1 developer can request and receive a usable C# example for an approved integration scenario.

## Risks and Constraints

1. Local LLM latency may be high for larger prompts or slower hardware.
2. SAP Service Layer access patterns and response shapes may vary by environment and require careful adapter design.
3. DI API is Windows-specific and should not shape the primary web-hosting design.
4. Prompt quality will materially affect usefulness, safety, and answer consistency.
5. SQLite and in-memory cache are appropriate for v1 but not the final scaling posture.
6. Generated code quality will depend heavily on prompt discipline and the specificity of supported SAP B1 coding patterns.

## Technical Product Decisions For V1

- framework target: `net10.0`
- UI: Blazor Web chat interface
- API style: REST over HTTP
- LLM provider: Ollama local models
- persistence: SQLite
- cache: in-memory cache
- SAP integration priority: Service Layer first
- DI API position: optional later adapter behind the same boundary

## Release Readiness Criteria

1. The clean architecture solution scaffold is in place with all five projects.
2. The web UI can send a message and render the assistant response.
3. The API persists and reloads conversation history.
4. Ollama integration returns assistant text successfully.
5. At least one approved SAP lookup flow works end-to-end through Service Layer.
6. Logging, health checks, and failure handling are present for major dependencies.
7. At least one developer-assistance flow returns usable C# code for a supported SAP B1 application scenario.

## Open Questions For Later Iterations

1. When should authentication and role-based access control be introduced?
2. Which SAP workflows should graduate from read-only to controlled write actions?
3. When should vector search or retrieval augmentation be added?
4. When should caching move from in-memory to Redis?
5. When should streaming responses be introduced in the UI?
6. Which SAP B1 development patterns should be first-class templates in the developer mode?