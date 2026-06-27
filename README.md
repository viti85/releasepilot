# ReleasePilot

## Overview
ReleasePilot is a deployment promotion engine showing a clean implementation of:
- **Domain-Driven Design (DDD)**: Rich domain models expressing state transitions, pipeline invariants, and rules.
- **CQRS**: Clean separation of reads (Queries) and writes (Commands) using MediatR.
- **Domain Events**: Dispatched asynchronously to process audit logs.
- **Ports and Adapters**: Decoupling application logic from external systems.
- **Asynchronous Messaging**: Powered by MassTransit and RabbitMQ.

---

## Architecture
The system is divided into four concentric layers following Clean Architecture principles:

- **Domain**: Pure business entities, value objects, and domain invariants (e.g., `Promotion`). This layer has absolutely zero dependencies on external libraries or frameworks.
- **Application**: CQRS command and query handlers, validation logic, orchestration, and Port interfaces.
  - *Note on Ports*: All Port interfaces (e.g., `IDeploymentPort`, `INotificationPort`, `IIssueTrackerPort`) reside in the **Application** layer, not the Domain layer. This enforces Dependency Inversion—the Domain layer remains completely isolated from external concepts, while the Application layer defines the contracts for the external services it orchestrates.
- **Infrastructure**: Implementations of repositories (`ReleasePilotDbContext`), MassTransit bus wiring, and Adapter implementations (including in-memory stubs and database-backed audit logging).
- **Api**: Entry point exposing Minimal API endpoints (extension methods), configuration setup, and global exception mapping middleware (translating exceptions to standard RFC 7807 `ProblemDetails` payloads).

---

## Prerequisites
- **.NET 10 SDK** or newer
- **Docker and Docker Compose**
- **dotnet-ef CLI Tool** (Install globally via `dotnet tool install --global dotnet-ef` if not already installed)

---

## Getting started

Follow these steps to run the application end-to-end:

### 1. Clone the repository
Navigate to the root directory containing the `All.slnx` file.

### 2. Start the infrastructure containers
Spins up PostgreSQL and RabbitMQ in the background:
```bash
docker compose up -d
```

### 3. Apply database migrations
Apply database schema updates to set up PostgreSQL tables:
```bash
dotnet ef database update --project src/ReleasePilot.Infrastructure --startup-project src/ReleasePilot.Api
```

### 4. Run the API project
Start the Minimal API application:
```bash
dotnet run --project src/ReleasePilot.Api
```
The server starts listening on `http://localhost:5286` and `https://localhost:5001`.

### 5. Verify system readiness
- **Readiness check**: Open `http://localhost:5286/health/ready` in your browser. It should return a `Healthy` status (HTTP 200) once Postgres and RabbitMQ are fully reachable.
- **Liveness check**: Open `http://localhost:5286/health/live` (always returns HTTP 200).
- **Scalar Documentation**: Browse `http://localhost:5286/scalar/v1` to load the interactive API Reference dashboard.

---

## Example requests

Use the following `curl` commands in order to execute the full promotion lifecycle:

### 1. Request a promotion to Dev
```bash
curl -X POST http://localhost:5286/promotions \
  -H "Content-Type: application/json" \
  -d '{
    "applicationId": "f7a3f3a8-48b4-4b53-a5c9-94b2a8d388ab",
    "version": "1.0.0",
    "targetEnvironment": "Dev",
    "requestedByUserId": "5f31c2a3-f09b-432d-9488-81203d9cb8a9"
  }'
```
**Expected Response (201 Created):**
```json
{
  "id": "e43b1778-f7b5-4b47-b765-df0b2df28a7b"
}
```

### 2. Approve the promotion
*Replace `e43b1778-f7b5-4b47-b765-df0b2df28a7b` with the ID returned in step 1.*
```bash
curl -X POST http://localhost:5286/promotions/e43b1778-f7b5-4b47-b765-df0b2df28a7b/approve \
  -H "Content-Type: application/json" \
  -d '{
    "approverId": "2c253d82-f674-4b53-a5c9-94b2a8d388ab",
    "approverRoles": ["approver"]
  }'
```
**Expected Response (200 OK):**
```json
{
  "id": "e43b1778-f7b5-4b47-b765-df0b2df28a7b",
  "status": "Approved"
}
```

### 3. Start deployment
```bash
curl -X POST http://localhost:5286/promotions/e43b1778-f7b5-4b47-b765-df0b2df28a7b/start \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "5f31c2a3-f09b-432d-9488-81203d9cb8a9"
  }'
```
**Expected Response (200 OK):**
```json
{
  "id": "e43b1778-f7b5-4b47-b765-df0b2df28a7b",
  "status": "InProgress"
}
```

### 4. Complete the promotion
```bash
curl -X POST http://localhost:5286/promotions/e43b1778-f7b5-4b47-b765-df0b2df28a7b/complete \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "5f31c2a3-f09b-432d-9488-81203d9cb8a9"
  }'
```
**Expected Response (200 OK):**
```json
{
  "id": "e43b1778-f7b5-4b47-b765-df0b2df28a7b",
  "status": "Completed"
}
```

### 5. Request a promotion to Staging (Dev must be completed first)
```bash
curl -X POST http://localhost:5286/promotions \
  -H "Content-Type: application/json" \
  -d '{
    "applicationId": "f7a3f3a8-48b4-4b53-a5c9-94b2a8d388ab",
    "version": "1.0.0",
    "targetEnvironment": "Staging",
    "requestedByUserId": "5f31c2a3-f09b-432d-9488-81203d9cb8a9"
  }'
```
**Expected Response (201 Created):**
```json
{
  "id": "70bbad12-a7d1-432b-91cc-88da29dfab12"
}
```

### 6. Get promotion detail
```bash
curl -X GET http://localhost:5286/promotions/70bbad12-a7d1-432b-91cc-88da29dfab12
```
**Expected Response (200 OK):**
```json
{
  "id": "70bbad12-a7d1-432b-91cc-88da29dfab12",
  "applicationId": "f7a3f3a8-48b4-4b53-a5c9-94b2a8d388ab",
  "version": "1.0.0",
  "targetEnvironment": "Staging",
  "status": "Pending",
  "requestedBy": "5f31c2a3-f09b-432d-9488-81203d9cb8a9",
  "approvedBy": null,
  "requestedAt": "2026-06-27T19:21:00Z",
  "completedAt": null,
  "stateHistory": [
    {
      "status": "Pending",
      "timestamp": "2026-06-27T19:21:00Z",
      "userId": "5f31c2a3-f09b-432d-9488-81203d9cb8a9"
    }
  ]
}
```

### 7. Get application status
```bash
curl -X GET http://localhost:5286/applications/f7a3f3a8-48b4-4b53-a5c9-94b2a8d388ab/status
```
**Expected Response (200 OK):**
```json
{
  "applicationId": "f7a3f3a8-48b4-4b53-a5c9-94b2a8d388ab",
  "environments": {
    "Dev": {
      "lastCompletedVersion": "1.0.0",
      "activePromotion": null
    },
    "Staging": {
      "lastCompletedVersion": null,
      "activePromotion": {
        "id": "70bbad12-a7d1-432b-91cc-88da29dfab12",
        "version": "1.0.0",
        "targetEnvironment": "Staging",
        "status": "Pending",
        "requestedAt": "2026-06-27T19:21:00Z"
      }
    },
    "Production": {
      "lastCompletedVersion": null,
      "activePromotion": null
    }
  }
}
```

### 8. Get promotion history (paginated)
```bash
curl -X GET "http://localhost:5286/applications/f7a3f3a8-48b4-4b53-a5c9-94b2a8d388ab/promotions?page=1&pageSize=2"
```
**Expected Response (200 OK):**
```json
{
  "items": [
    {
      "id": "70bbad12-a7d1-432b-91cc-88da29dfab12",
      "version": "1.0.0",
      "targetEnvironment": "Staging",
      "status": "Pending",
      "requestedAt": "2026-06-27T19:21:00Z"
    },
    {
      "id": "e43b1778-f7b5-4b47-b765-df0b2df28a7b",
      "version": "1.0.0",
      "targetEnvironment": "Dev",
      "status": "Completed",
      "requestedAt": "2026-06-27T19:20:00Z"
    }
  ],
  "page": 1,
  "pageSize": 2,
  "totalCount": 2,
  "totalPages": 1
}
```

### 9. Attempt to skip an environment
```bash
curl -X POST http://localhost:5286/promotions \
  -H "Content-Type: application/json" \
  -d '{
    "applicationId": "f7a3f3a8-48b4-4b53-a5c9-94b2a8d388ab",
    "version": "2.0.0",
    "targetEnvironment": "Production",
    "requestedByUserId": "5f31c2a3-f09b-432d-9488-81203d9cb8a9"
  }'
```
**Expected Response (422 Unprocessable Entity):**
```json
{
  "type": "https://releasepilot.dev/errors/environment-skipped",
  "title": "Environment skipped",
  "status": 422,
  "detail": "Promotion to environment 'Production' is not allowed for application 'f7a3f3a8-48b4-4b53-a5c9-94b2a8d388ab' and version '2.0.0'. Prerequisite environment 'Staging' must be completed first.",
  "instance": "/promotions"
}
```

### 10. Attempt to approve as non-approver
```bash
curl -X POST http://localhost:5286/promotions/70bbad12-a7d1-432b-91cc-88da29dfab12/approve \
  -H "Content-Type: application/json" \
  -d '{
    "approverId": "2c253d82-f674-4b53-a5c9-94b2a8d388ab",
    "approverRoles": []
  }'
```
**Expected Response (403 Forbidden):**
```json
{
  "type": "https://releasepilot.dev/errors/unauthorized-approval",
  "title": "Unauthorized approval",
  "status": 403,
  "detail": "User '2c253d82-f674-4b53-a5c9-94b2a8d388ab' is not authorized to approve promotions.",
  "instance": "/promotions/70bbad12-a7d1-432b-91cc-88da29dfab12/approve"
}
```

---

## Running the tests
Run the unit, application, and integration test suites:
```bash
dotnet test All.slnx --verbosity normal
```

---

## Design decisions

- **Port Interfaces in Application Layer**: Enforces the Dependency Inversion Principle. The Domain layer remains simple and contains only core state machines and aggregates, completely unaffected by external infrastructure patterns.
- **Separate Database Entities from Domain Aggregate**: Decoupled `Promotion` aggregate from EF Core entity tracking attributes (`PromotionEntity`, `PromotionStateTransitionEntity`). This allows domain concepts to change independently of database schema considerations, keeping persistence concerns localized within the Infrastructure adapters.
- **Domain Event Publication After Saving**: In `PromotionRepository`, domain events are published using MassTransit only *after* aggregate changes have successfully been written to the database. This prevents publishing integration events for database transactions that subsequently fail due to concurrency conflicts or constraints.
- **Future Improvements**:
  - **Transactional Outbox Pattern**: Store domain events inside the database transaction and publish them asynchronously via a worker process to provide reliable execution guarantees.
  - **Production Broker Topology**: Configure persistent exchange-queue layouts in RabbitMQ rather than rely on the default direct bindings.
  - **Fine-Grained Auditing**: Enrich the audit trail log records with specific action context payload definitions.

---

## AI Session Logs & Logs Reference
All AI session logs, prompt histories, observations, and raw session transcripts are saved inside the [ai-session/](file:///c:/Code/releasepilot/ai-session/) folder:
- [PROMPTS.md](file:///c:/Code/releasepilot/ai-session/PROMPTS.md): Catalog of prompts and AI modifications/corrections.
- [OBSERVATIONS.md](file:///c:/Code/releasepilot/ai-session/OBSERVATIONS.md): Breakdown of challenges, resolutions, and compiler details.
- [Raw Transcripts](file:///c:/Code/releasepilot/ai-session/raw/): Contains the raw JSONL session logs.
