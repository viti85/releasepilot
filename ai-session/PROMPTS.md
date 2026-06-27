# ReleasePilot — AI Prompts Log

All prompts used during implementation, grouped by phase.

---

## Phase 1 — Pure domain + CQRS + tests

### Prompt 1 — Full scaffolding from scratch

```
Create a complete .NET 10 solution for a project called ReleasePilot from scratch.
Show me every shell command in order so I can run them sequentially.

---

STEP 1 — Solution

Create the solution file in the current directory:
  dotnet new sln --name All

---

STEP 2 — Projects

Create the following projects with the exact paths shown.
Use --framework net10.0 on every dotnet new command.

  src/ReleasePilot.Domain         (classlib)
  src/ReleasePilot.Application    (classlib)
  src/ReleasePilot.Infrastructure (classlib)
  src/ReleasePilot.Api            (webapi, --use-minimal-api, no controllers)

  tests/ReleasePilot.Tests.Domain       (xunit)
  tests/ReleasePilot.Tests.Application  (xunit)

---

STEP 3 — Project references

  ReleasePilot.Application    → ReleasePilot.Domain
  ReleasePilot.Infrastructure → ReleasePilot.Application
  ReleasePilot.Api            → ReleasePilot.Application
  ReleasePilot.Api            → ReleasePilot.Infrastructure

  ReleasePilot.Tests.Domain       → ReleasePilot.Domain
  ReleasePilot.Tests.Application  → ReleasePilot.Application
  ReleasePilot.Tests.Application  → ReleasePilot.Domain

---

STEP 4 — Register all projects in the solution

Add all six projects to All.sln using dotnet sln add.
Group them by folder (src and tests) in the solution.

---

STEP 5 — Global files at the solution root

Create these four files exactly as specified:

1. global.json
{
  "sdk": {
    "version": "10.0.0",
    "rollForward": "latestMinor"
  }
}

2. Directory.Build.props
Apply to every project automatically:
- TargetFramework: net10.0
- Nullable: enable
- ImplicitUsings: enable
- TreatWarningsAsErrors: true
- LangVersion: latest

Remove the TargetFramework property from every individual .csproj after
creating this file — it is now managed globally.

3. Directory.Packages.props
Enable central package management (ManagePackageVersionsCentrally = true).
Versions are declared here only — individual .csproj files use
PackageReference with no Version attribute.

Packages and their latest stable versions compatible with .NET 10:
  - MediatR
  - FluentValidation
  - FluentValidation.DependencyInjectionExtensions
  - Microsoft.AspNetCore.OpenApi
  - Scalar.AspNetCore
  - xunit
  - xunit.runner.visualstudio
  - Microsoft.NET.Test.Sdk
  - FluentAssertions
  - NSubstitute
  - coverlet.collector

4. .editorconfig
  root = true
  [*.cs]
  indent_style = space
  indent_size = 4
  end_of_line = lf
  charset = utf-8-bom
  insert_final_newline = true
  dotnet_sort_system_directives_first = true
  csharp_style_expression_bodied_methods = when_on_single_line:suggestion
  csharp_new_line_before_open_brace = all

---

STEP 6 — Add NuGet packages

Add packages to each project using dotnet add package (no version numbers).
Versions are resolved from Directory.Packages.props.

  src/ReleasePilot.Application:
    MediatR
    FluentValidation
    FluentValidation.DependencyInjectionExtensions

  src/ReleasePilot.Api:
    Microsoft.AspNetCore.OpenApi
    Scalar.AspNetCore

  tests/ReleasePilot.Tests.Domain:
    xunit
    xunit.runner.visualstudio
    Microsoft.NET.Test.Sdk
    FluentAssertions
    coverlet.collector

  tests/ReleasePilot.Tests.Application:
    xunit
    xunit.runner.visualstudio
    Microsoft.NET.Test.Sdk
    FluentAssertions
    NSubstitute
    coverlet.collector

Do NOT add any package to ReleasePilot.Domain or ReleasePilot.Infrastructure.
Do NOT add Entity Framework, MassTransit, or any database package yet.

---

STEP 7 — Verify

Run these two commands and show me the output:
  dotnet build All.sln
  dotnet test All.sln

Then print the full directory tree of the solution so I can verify the structure.
Expected shape:
  /
  ├── All.sln
  ├── global.json
  ├── Directory.Build.props
  ├── Directory.Packages.props
  ├── .editorconfig
  ├── src/
  │   ├── ReleasePilot.Domain/
  │   ├── ReleasePilot.Application/
  │   ├── ReleasePilot.Infrastructure/
  │   └── ReleasePilot.Api/
  └── tests/
      ├── ReleasePilot.Tests.Domain/
      └── ReleasePilot.Tests.Application/
```

---

### Prompt 2 — Value objects and domain exceptions

```
We are building the Domain layer of ReleasePilot. No external dependencies are allowed here.

Create the following in ReleasePilot.Domain:

1. Value objects (immutable records):
   - PromotionId (wraps Guid)
   - ApplicationId (wraps Guid)
   - AppVersion (wraps string, non-empty)

2. Environment enum — ordered pipeline, must be comparable:
   - Dev = 1, Staging = 2, Production = 3

3. PromotionStatus enum:
   - Pending, Approved, InProgress, Completed, RolledBack, Cancelled

4. Domain exceptions — all inherit from DomainException (base class):
   - EnvironmentSkippedException: thrown when trying to promote to an environment
     before completing the previous one
   - ConcurrentPromotionException: thrown when there is already an active promotion
     for the same application + target environment
   - ImmutablePromotionException: thrown when trying to mutate a completed or cancelled promotion
   - UnauthorizedApprovalException: thrown when a non-approver tries to approve

Keep everything in the Domain project. No MediatR, no EF Core attributes.
```

---

### Prompt 3 — AggregateRoot base and domain events

```
In ReleasePilot.Domain, create the building blocks for DDD aggregates:

1. Abstract class AggregateRoot:
   - Holds a private List<IDomainEvent> internally
   - Exposes IReadOnlyCollection<IDomainEvent> DomainEvents
   - Protected method Raise(IDomainEvent @event) that adds to the list
   - Method ClearDomainEvents() called after publishing

2. Marker interface IDomainEvent with:
   - Guid EventId (default: Guid.NewGuid())
   - DateTime OccurredAt (default: DateTime.UtcNow)
   - Guid PromotionId
   - Guid ActingUserId

3. Domain events as records implementing IDomainEvent:
   - PromotionRequestedEvent
   - PromotionApprovedEvent
   - DeploymentStartedEvent
   - PromotionCompletedEvent
   - PromotionRolledBackEvent
   - PromotionCancelledEvent

Each event carries only the data relevant to that transition.
No external dependencies.
```

---

### Prompt 4 — Promotion aggregate

```
Create the Promotion aggregate in ReleasePilot.Domain.
This is the most important class — it must enforce all business invariants itself.

Requirements:
- Private constructor. Instantiation only via static factory method.
- Properties: Id, ApplicationId, Version, TargetEnvironment, Status, RequestedBy,
  ApprovedBy, RequestedAt, CompletedAt
- State history: a private list of PromotionStateTransition value objects
  (Status, Timestamp, UserId) exposed as IReadOnlyList<PromotionStateTransition>

Static factory method:
  Promotion.Request(ApplicationId, AppVersion, Environment target,
                    Guid requestedByUserId,
                    IReadOnlyList<Promotion> activePromotions,
                    IReadOnlyList<Promotion> completedPromotionsForApp)

  Enforces:
  - If target is Staging, there must be a completed Promotion for Dev with same version
  - If target is Production, there must be a completed Promotion for Staging with same version
  - No active (non-terminal) promotion exists for same ApplicationId + TargetEnvironment

State transition methods (each raises its corresponding domain event):
  - Approve(Guid approverId, IReadOnlyCollection<string> approverRoles)
    → validates role contains "approver", status must be Pending
  - StartDeployment(Guid userId)
    → status must be Approved
  - Complete(Guid userId)
    → status must be InProgress
  - Rollback(Guid userId, string reason)
    → status must be InProgress
  - Cancel(Guid userId)
    → status must be Pending or Approved

Terminal states (Completed, RolledBack, Cancelled) are immutable —
any mutation attempt throws ImmutablePromotionException.

Do not inject any interface or service. Pass what the aggregate needs as method parameters.
```

---

### Prompt 5 — Repository and port interfaces

```
In ReleasePilot.Application, define the following interfaces.
These are contracts only — no implementations yet.

1. IPromotionRepository:
   Task<Promotion?> GetByIdAsync(PromotionId id, CancellationToken ct);
   Task<IReadOnlyList<Promotion>> GetActiveByApplicationAsync(ApplicationId appId, CancellationToken ct);
   Task<IReadOnlyList<Promotion>> GetCompletedByApplicationAsync(ApplicationId appId, CancellationToken ct);
   Task SaveAsync(Promotion promotion, CancellationToken ct);

2. IEventBus:
   Task PublishAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken ct);

3. Port interfaces (in Application/Ports/):
   IDeploymentPort — Task TriggerAsync(PromotionId id, CancellationToken ct)
   INotificationPort — Task NotifyTerminalStateAsync(PromotionId id, PromotionStatus status, CancellationToken ct)
   IIssueTrackerPort — Task<IReadOnlyList<WorkItemDto>> GetLinkedItemsAsync(PromotionId id, CancellationToken ct)

Include WorkItemDto as a simple record with Id, Title, Url.

Important: these interfaces live in Application, not Domain.
The Domain aggregate does not know they exist.
Be ready to explain why in a code review.
```

---

### Prompt 6 — Commands and CQRS handlers

```
In ReleasePilot.Application, implement the write side using MediatR.
One record command + one handler class per use case.

Commands (records implementing IRequest<T>):
  - RequestPromotionCommand → returns PromotionId
  - ApprovePromotionCommand → returns Unit
  - StartDeploymentCommand  → returns Unit
  - CompletePromotionCommand → returns Unit
  - RollbackPromotionCommand (include Reason string) → returns Unit
  - CancelPromotionCommand → returns Unit

Each handler must:
1. Load the aggregate via IPromotionRepository
2. Call the corresponding method on the aggregate
3. Save via repository
4. Publish domain events via IEventBus
5. Call the appropriate port if needed
   (StartDeployment → IDeploymentPort, Cancel/Rollback/Complete → INotificationPort)

The handler must NOT contain any business logic.
All rules live in the aggregate.

Also add FluentValidation validators for each command:
  - RequestPromotionCommand: ApplicationId not empty, Version not empty, TargetEnvironment valid
  - ApprovePromotionCommand: PromotionId not empty, ApproverId not empty
  - etc.

Wire the ValidationBehavior<TRequest, TResponse> into the MediatR pipeline.
```

---

### Prompt 7 — Queries and read models

```
In ReleasePilot.Application, implement the read side using MediatR.

Queries and their handlers:

1. GetPromotionByIdQuery(PromotionId)
   → returns PromotionDetailDto or null
   PromotionDetailDto: Id, ApplicationId, Version, TargetEnvironment, Status,
                       RequestedBy, ApprovedBy, RequestedAt, CompletedAt,
                       List<StateTransitionDto> History

2. GetApplicationStatusQuery(ApplicationId)
   → returns ApplicationStatusDto
   ApplicationStatusDto: ApplicationId,
                         Dictionary<Environment, EnvironmentStatusDto> Environments
   EnvironmentStatusDto: LastCompletedVersion (string?), ActivePromotion (PromotionSummaryDto?)

3. GetPromotionHistoryQuery(ApplicationId, int Page, int PageSize)
   → returns PagedResult<PromotionSummaryDto>
   PromotionSummaryDto: Id, Version, TargetEnvironment, Status, RequestedAt
   PagedResult<T>: Items, Page, PageSize, TotalCount

Read models must be shaped for the consumer, not for the aggregate.
Handlers read directly from IPromotionRepository — do NOT reuse the aggregate
for projections. Keep the read path thin and fast.
```

---

### Prompt 8 — Domain tests

```
In ReleasePilot.Tests.Domain, write xUnit tests for the Promotion aggregate.
Use FluentAssertions for assertions. No mocks needed — pure domain logic only.

Test classes to create:

1. PromotionInvariantTests
   - Requesting staging promotion without completed dev → throws EnvironmentSkippedException
   - Requesting production promotion without completed staging → throws EnvironmentSkippedException
   - Requesting a promotion when one is already active for same app+env → throws ConcurrentPromotionException
   - Approving with a user without "approver" role → throws UnauthorizedApprovalException
   - Mutating a completed promotion → throws ImmutablePromotionException
   - Mutating a cancelled promotion → throws ImmutablePromotionException
   - Mutating a rolled-back promotion → throws ImmutablePromotionException

2. PromotionStateMachineTests
   - Full happy path: Request → Approve → StartDeployment → Complete
   - Full rollback path: Request → Approve → StartDeployment → Rollback
   - Cancel from Pending state
   - Cancel from Approved state
   - Each transition emits the correct domain event type
   - State history records each transition with correct status and timestamp

3. EnvironmentPipelineTests
   - Dev can be requested with no prerequisites
   - Staging requires completed Dev for same version
   - Production requires completed Staging for same version
   - A completed promotion for a different version does NOT unblock the next environment

All tests must pass without touching any database, queue, or HTTP client.
```

---

### Prompt 9 — Application layer tests

```
In ReleasePilot.Tests.Application, write xUnit tests for the command handlers.
Use NSubstitute for mocking interfaces. Use FluentAssertions for assertions.

Test classes to create:

1. RequestPromotionHandlerTests
   - Happy path: handler calls repository.SaveAsync() with a Promotion in Pending status
   - Happy path: handler calls eventBus.PublishAsync() with a PromotionRequestedEvent
   - When aggregate throws EnvironmentSkippedException, handler lets it propagate (no swallowing)
   - When aggregate throws ConcurrentPromotionException, handler lets it propagate

2. ApprovePromotionHandlerTests
   - Happy path: aggregate transitions to Approved, repository saves, events published
   - Promotion not found: handler throws NotFoundException (create this exception in Application)
   - User is not an approver: UnauthorizedApprovalException propagates

3. ValidationBehaviorTests
   - Sending RequestPromotionCommand with empty ApplicationId → throws ValidationException
     before the handler is ever called (verify handler is never invoked)
   - Sending a valid command → handler is called exactly once

Mock setup guidance:
  - Use NSubstitute's Arg.Any<CancellationToken>() for cancellation tokens
  - Set up GetActiveByApplicationAsync and GetCompletedByApplicationAsync
    to return empty lists for the happy path tests
  - Verify repository.SaveAsync() was called using NSubstitute's Received()
```

---

## Phase 2 — Infrastructure

### Prompt 1 — Infrastructure NuGet packages

```
We are starting Phase 2 of ReleasePilot. The domain and application layers are complete.
Now we need to add the infrastructure NuGet packages.

Add the following packages to Directory.Packages.props (versions section only,
no Version attribute in .csproj files):

  - Microsoft.EntityFrameworkCore
  - Microsoft.EntityFrameworkCore.Design
  - Npgsql.EntityFrameworkCore.PostgreSQL
  - MassTransit
  - MassTransit.RabbitMQ
  - MassTransit.EntityFrameworkCore

Add references to the projects:

  src/ReleasePilot.Infrastructure:
    - Microsoft.EntityFrameworkCore
    - Npgsql.EntityFrameworkCore.PostgreSQL
    - MassTransit
    - MassTransit.RabbitMQ
    - MassTransit.EntityFrameworkCore

  src/ReleasePilot.Api:
    - Microsoft.EntityFrameworkCore.Design

  tests/ReleasePilot.Tests.Domain:
    (no changes)

  tests/ReleasePilot.Tests.Application:
    (no changes)

Add these test packages to Directory.Packages.props and to
tests/ReleasePilot.Tests.Infrastructure (create this new xUnit project
under tests/, add it to All.sln, and add project references to
ReleasePilot.Domain, ReleasePilot.Application and ReleasePilot.Infrastructure):

  - Testcontainers
  - Testcontainers.PostgreSql
  - MassTransit.Testing

Run dotnet build All.sln and confirm it compiles before moving on.
```

---

### Prompt 2 — DbContext and Fluent API configuration

```
In ReleasePilot.Infrastructure, create the EF Core DbContext.

1. Create ReleasePilotDbContext inheriting from DbContext:
   - DbSet<PromotionEntity> Promotions
   - DbSet<PromotionStateTransitionEntity> StateTransitions
   - DbSet<AuditLogEntry> AuditLog
   - Override OnModelCreating to apply all entity configurations

2. Do NOT use data annotations on domain classes. All mapping goes in
   separate IEntityTypeConfiguration<T> classes:

   PromotionEntityConfiguration:
   - Table name: promotions
   - Primary key: Id (Guid)
   - All columns explicitly named in snake_case
   - ApplicationId, Version, TargetEnvironment, Status, RequestedBy,
     ApprovedBy (nullable), RequestedAt, CompletedAt (nullable)
   - HasMany StateTransitions with cascade delete

   PromotionStateTransitionEntityConfiguration:
   - Table name: promotion_state_transitions
   - Primary key: Id (Guid)
   - Foreign key: PromotionId
   - Columns: Status, OccurredAt, UserId

   AuditLogEntryConfiguration:
   - Table name: audit_log
   - Primary key: Id (Guid)
   - Columns: EventType, PromotionId, OccurredAt, ActingUserId, Payload (jsonb)

3. Create a separate set of persistence entities (PromotionEntity,
   PromotionStateTransitionEntity, AuditLogEntry) in Infrastructure/Persistence/Entities/.
   These are plain classes with no domain logic — they exist only for EF Core mapping.
   The domain Promotion aggregate stays clean with no EF attributes.

Do not create migrations yet — that comes in a later step.
```

---

### Prompt 3 — Value object converters

```
In ReleasePilot.Infrastructure/Persistence/Converters/, create EF Core value converters
for all domain value objects. Apply them inside the entity configurations.

Converters to create:

1. PromotionIdConverter
   - Converts PromotionId ↔ Guid
   - Column type: uuid

2. ApplicationIdConverter
   - Converts ApplicationId ↔ Guid
   - Column type: uuid

3. AppVersionConverter
   - Converts AppVersion ↔ string
   - Column type: varchar(100)

4. EnvironmentConverter
   - Converts Environment enum ↔ string (store as "Dev", "Staging", "Production")
   - Column type: varchar(20)
   - Do NOT store as int — string makes the database human-readable

5. PromotionStatusConverter
   - Converts PromotionStatus enum ↔ string
   - Column type: varchar(20)
   - Same reasoning: store as string, not int

Apply each converter in the corresponding IEntityTypeConfiguration using
.HasConversion(new XConverter()) on the relevant property.

After applying converters, verify there are no unmapped properties by running
dotnet build and checking for EF Core warnings.
```

---

### Prompt 4 — PromotionRepository

```
In ReleasePilot.Infrastructure/Persistence/, implement IPromotionRepository
using EF Core and ReleasePilotDbContext.

The repository must map between persistence entities (PromotionEntity)
and the domain aggregate (Promotion). Create a private mapper for this:

  PromotionEntity → Promotion (toDomain)
  Promotion → PromotionEntity (toPersistence)

Implement all methods from IPromotionRepository:

1. GetByIdAsync(PromotionId id, CancellationToken ct)
   - Include StateTransitions in the query
   - Return null if not found
   - Map PromotionEntity to domain Promotion aggregate

2. GetActiveByApplicationAsync(ApplicationId appId, CancellationToken ct)
   - Active means Status is NOT Completed, RolledBack, or Cancelled
   - Return as IReadOnlyList<Promotion>

3. GetCompletedByApplicationAsync(ApplicationId appId, CancellationToken ct)
   - Completed means Status IS Completed
   - Return as IReadOnlyList<Promotion>

4. SaveAsync(Promotion promotion, CancellationToken ct)
   - Check if entity already exists (insert vs update)
   - Map domain aggregate to PromotionEntity
   - Persist StateTransitions
   - Do NOT publish domain events here — that is the handler's responsibility

Important: the domain Promotion aggregate must be reconstructed without
calling its private constructor directly. Use a static factory method
Promotion.Reconstitute(...) that bypasses business rule validation
and is intended only for persistence reconstruction.
```

---

### Prompt 5 — Initial migration and docker-compose

```
Create the initial EF Core migration and the docker-compose.yml file.

1. Add a DesignTimeDbContextFactory in ReleasePilot.Infrastructure
   so that dotnet ef can instantiate the DbContext without the API running:

   ReleasePilotDbContextFactory implementing IDesignTimeDbContextFactory<ReleasePilotDbContext>
   - Reads connection string from environment variable RELEASEPILOT_DB
     or falls back to "Host=localhost;Port=5432;Database=releasepilot;Username=postgres;Password=dev"

2. Run the initial migration:
   dotnet ef migrations add InitialSchema \
     --project src/ReleasePilot.Infrastructure \
     --startup-project src/ReleasePilot.Api \
     --output-dir Persistence/Migrations

   Show me the generated migration and verify it contains tables for:
   - promotions
   - promotion_state_transitions
   - audit_log

3. Create docker-compose.yml at the solution root:

services:
  db:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: releasepilot
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: dev
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  rabbitmq:
    image: rabbitmq:3-management-alpine
    ports:
      - "5672:5672"
      - "15672:15672"

volumes:
  postgres_data:

4. Add an appsettings.Development.json to ReleasePilot.Api with:
   - ConnectionStrings:DefaultConnection pointing to the docker postgres
   - RabbitMq:Host, RabbitMq:Username, RabbitMq:Password matching docker-compose
```

---

### Prompt 6 — IEventBus with MassTransit

```
In ReleasePilot.Infrastructure, implement IEventBus using MassTransit with RabbitMQ.

1. Create MassTransitEventBus implementing IEventBus:
   - Inject IPublishEndpoint from MassTransit
   - In PublishAsync, iterate over the domain events collection
     and publish each one individually via publishEndpoint.Publish()
   - Clear domain events from the aggregate after publishing
     by calling promotion.ClearDomainEvents()

2. Create an extension method AddInfrastructure(this IServiceCollection services,
   IConfiguration configuration) in Infrastructure/DependencyInjection.cs that registers:
   - ReleasePilotDbContext with Npgsql and the connection string from configuration
   - IPromotionRepository → PromotionRepository (scoped)
   - IEventBus → MassTransitEventBus (scoped)
   - MassTransit with RabbitMQ:
       x.UsingRabbitMq((ctx, cfg) =>
       {
           cfg.Host(configuration["RabbitMq:Host"], h =>
           {
               h.Username(configuration["RabbitMq:Username"]);
               h.Password(configuration["RabbitMq:Password"]);
           });
           cfg.ConfigureEndpoints(ctx);
       })

3. In ReleasePilot.Api/Program.cs, call builder.Services.AddInfrastructure(builder.Configuration).

Do not register consumers yet — that comes in the next step.
```

---

### Prompt 7 — AuditLogConsumer

```
In ReleasePilot.Infrastructure/Messaging/Consumers/, create a MassTransit consumer
that persists every domain event to the audit_log table.

1. Create AuditLogConsumer implementing IConsumer<IDomainEvent>:
   - Inject ReleasePilotDbContext
   - On Consume(ConsumeContext<IDomainEvent> context):
       - Create a new AuditLogEntry with:
           Id: Guid.NewGuid()
           EventType: context.Message.GetType().Name
           PromotionId: context.Message.PromotionId
           OccurredAt: context.Message.OccurredAt
           ActingUserId: context.Message.ActingUserId
           Payload: serialize the full event to JSON (System.Text.Json)
       - Add to DbContext and SaveChangesAsync

2. Register the consumer in AddInfrastructure:
   x.AddConsumer<AuditLogConsumer>()

   And configure its receive endpoint in UsingRabbitMq:
   cfg.ReceiveEndpoint("audit-log", e =>
       e.ConfigureConsumer<AuditLogConsumer>(ctx))

3. The API must respond before the consumer finishes.
   The handler publishes the event and returns immediately.
   The consumer runs asynchronously in a background process.
   Do not await the consumer from the handler — MassTransit handles this.

4. Add a separate IConsumer for each domain event type if needed,
   or use a single generic consumer — justify your choice in a comment.
```

---

### Prompt 8 — In-memory port stubs

```
In ReleasePilot.Infrastructure/Adapters/, create in-memory stub implementations
for all three port interfaces defined in Application/Ports/.

1. InMemoryDeploymentPort implementing IDeploymentPort:
   - TriggerAsync: log to ILogger that deployment was triggered for promotionId
   - Return Task.CompletedTask
   - Add a configurable delay (default 0ms) to simulate async work

2. InMemoryNotificationPort implementing INotificationPort:
   - NotifyTerminalStateAsync: log to ILogger the promotionId and terminal status
   - Keep an internal ConcurrentBag<string> NotificationsSent for test assertions
   - Return Task.CompletedTask

3. InMemoryIssueTrackerPort implementing IIssueTrackerPort:
   - GetLinkedItemsAsync: return a hardcoded list of two WorkItemDto stubs
       new WorkItemDto("ISSUE-1", "Add login feature", "https://issues.example.com/1")
       new WorkItemDto("ISSUE-2", "Fix signup bug", "https://issues.example.com/2")

4. Register all three stubs in AddInfrastructure:
   services.AddScoped<IDeploymentPort, InMemoryDeploymentPort>()
   services.AddScoped<INotificationPort, InMemoryNotificationPort>()
   services.AddScoped<IIssueTrackerPort, InMemoryIssueTrackerPort>()

These stubs are the real adapters for now. No real HTTP calls.
The interfaces are the boundary — swapping stubs for real implementations
requires no changes to the Application layer.
```

---

### Prompt 9 — Repository tests with Testcontainers

```
In ReleasePilot.Tests.Infrastructure, write integration tests for PromotionRepository
using Testcontainers to spin up a real PostgreSQL instance.

1. Create a shared PostgreSqlContainerFixture implementing IAsyncLifetime:
   - Starts a PostgreSqlContainer (Testcontainers.PostgreSql) in InitializeAsync
   - Applies EF Core migrations against the container in InitializeAsync
   - Exposes the ConnectionString
   - Stops and disposes the container in DisposeAsync

2. Create PromotionRepositoryTests using the fixture (IClassFixture<PostgreSqlContainerFixture>):

   Each test must:
   - Create a fresh ReleasePilotDbContext scoped to the test
   - Clean relevant tables before each test using a helper method

   Tests to write:

   - SaveAsync_and_GetById_roundtrip
     Create a Promotion via Promotion.Request(), save it, retrieve it by id,
     assert all properties match including TargetEnvironment and Status

   - GetActiveByApplication_excludes_terminal_states
     Save one Pending and one Completed promotion for the same ApplicationId,
     call GetActiveByApplicationAsync, assert only the Pending one is returned

   - GetCompletedByApplication_returns_only_completed
     Save promotions in Pending, InProgress, Completed, Cancelled states,
     assert only the Completed one is returned

   - SaveAsync_persists_state_transitions
     Create a Promotion, approve it, save it, retrieve it,
     assert StateTransitions contains both the initial Pending and the Approved transition

   - Reconstitute_preserves_domain_events_cleared
     After saving and reloading a promotion, assert DomainEvents is empty
     (events were cleared after publishing, not re-raised on load)

3. Do not use InMemory EF Core provider — always use the real PostgreSQL container.
   InMemory does not enforce constraints and gives false confidence.
```

---

### Prompt 10 — AuditLogConsumer tests with MassTransit harness

```
In ReleasePilot.Tests.Infrastructure, write tests for AuditLogConsumer
using the MassTransit in-memory test harness. No real RabbitMQ needed here.

1. Create AuditLogConsumerTests using the MassTransit testing harness:
   - Use ServiceCollection to register the DbContext with a real PostgreSQL
     container (reuse PostgreSqlContainerFixture from Step 9)
   - Register AuditLogConsumer
   - Use AddMassTransitTestHarness(x => x.AddConsumer<AuditLogConsumer>())

2. Tests to write:

   - Consume_PromotionApprovedEvent_persists_audit_entry
     Publish a PromotionApprovedEvent via harness.Bus.Publish()
     Wait for consumer to finish: await harness.Consumed.Any<PromotionApprovedEvent>()
     Query the AuditLog table and assert:
       - One entry exists with EventType = "PromotionApprovedEvent"
       - PromotionId matches
       - ActingUserId matches
       - Payload is valid JSON

   - Consume_multiple_events_persists_all_entries
     Publish PromotionRequestedEvent and PromotionApprovedEvent sequentially
     Assert two entries exist in the AuditLog table

   - Consumer_does_not_block_publisher
     Publish an event and assert the publish call returns before
     harness.Consumed.Any() resolves (fire and forget semantics)

3. Use harness.Consumed.Any<T>() with a timeout (default 5s) to avoid
   flaky tests from timing issues.

4. After all tests pass, run the full test suite:
   dotnet test All.sln --verbosity normal

   All tests from Phase 1 must still pass. If any Phase 1 test breaks,
   fix the regression before moving to Phase 3.
```

---

## Phase 3 — API + end-to-end integration

### Prompt 1 — Command endpoints

```
In ReleasePilot.Api, implement the command endpoints using Minimal API.
No controllers — everything goes in endpoint extension methods.

1. Create an extension method AddCommandEndpoints(this WebApplication app)
   in Api/Endpoints/CommandEndpoints.cs

2. Endpoints to implement:

   POST /promotions
   - Body: { applicationId, version, targetEnvironment, requestedByUserId }
   - Dispatches RequestPromotionCommand via IMediator
   - Returns 201 Created with Location header pointing to /promotions/{id}
   - Body: { id }

   POST /promotions/{id}/approve
   - Body: { approverId, approverRoles: string[] }
   - Dispatches ApprovePromotionCommand
   - Returns 200 OK with { id, status: "Approved" }

   POST /promotions/{id}/start
   - Body: { userId }
   - Dispatches StartDeploymentCommand
   - Returns 200 OK with { id, status: "InProgress" }

   POST /promotions/{id}/complete
   - Body: { userId }
   - Dispatches CompletePromotionCommand
   - Returns 200 OK with { id, status: "Completed" }

   POST /promotions/{id}/rollback
   - Body: { userId, reason }
   - Dispatches RollbackPromotionCommand
   - Returns 200 OK with { id, status: "RolledBack" }

   POST /promotions/{id}/cancel
   - Body: { userId }
   - Dispatches CancelPromotionCommand
   - Returns 200 OK with { id, status: "Cancelled" }

3. All endpoints receive and return JSON.
   Use record types for request bodies — one record per endpoint in Api/Endpoints/Requests/.
   Do not reuse Application command records as HTTP request bodies.

4. Call app.AddCommandEndpoints() from Program.cs.
```

---

### Prompt 2 — Query endpoints

```
In ReleasePilot.Api, implement the query endpoints using Minimal API.
Add them in Api/Endpoints/QueryEndpoints.cs.

1. Create an extension method AddQueryEndpoints(this WebApplication app)

2. Endpoints to implement:

   GET /promotions/{id}
   - Dispatches GetPromotionByIdQuery via IMediator
   - Returns 200 OK with PromotionDetailDto
   - Returns 404 Not Found if promotion does not exist
   - Response includes full state history array

   GET /applications/{id}/status
   - Dispatches GetApplicationStatusQuery
   - Returns 200 OK with ApplicationStatusDto
   - Shows current state per environment (Dev, Staging, Production)
   - Each environment shows lastCompletedVersion and activePromotion (nullable)

   GET /applications/{id}/promotions
   - Dispatches GetPromotionHistoryQuery
   - Accepts query parameters: page (default 1), pageSize (default 20, max 100)
   - Returns 200 OK with PagedResult<PromotionSummaryDto>
   - Include pagination metadata in response body:
       { items, page, pageSize, totalCount, totalPages }

3. Use record types for any response shaping needed beyond the DTOs
   already defined in Application.

4. Call app.AddQueryEndpoints() from Program.cs.
```

---

### Prompt 3 — Global exception middleware

```
In ReleasePilot.Api, create a global exception handling middleware
that maps domain exceptions to the correct HTTP status codes.

1. Create DomainExceptionMiddleware in Api/Middleware/:
   - Wraps the next middleware in a try/catch
   - Maps exceptions to HTTP responses using ProblemDetails (RFC 7807)

2. Exception mapping:

   DomainException (base)         → 422 Unprocessable Entity
   EnvironmentSkippedException    → 422 with detail message
   ConcurrentPromotionException   → 409 Conflict
   ImmutablePromotionException    → 409 Conflict
   UnauthorizedApprovalException  → 403 Forbidden
   NotFoundException              → 404 Not Found
   ValidationException            → 400 Bad Request
     (include all validation errors in the response body)
   Any other exception            → 500 Internal Server Error
     (do not leak stack traces — log them, return generic message)

3. ProblemDetails response shape:
   {
     "type": "https://releasepilot.dev/errors/environment-skipped",
     "title": "Environment skipped",
     "status": 422,
     "detail": "Version 1.4 has not completed staging",
     "instance": "/promotions"
   }

4. Register the middleware in Program.cs before app.MapGroup or any endpoint:
   app.UseMiddleware<DomainExceptionMiddleware>()

5. Add an ILogger<DomainExceptionMiddleware> and log every 5xx as Error,
   every 4xx as Warning. Never log stack traces for domain exceptions.
```

---

### Prompt 4 — Scalar API documentation

```
In ReleasePilot.Api, configure Scalar as the API documentation UI.
It is already added as a NuGet package — now wire it up.

1. In Program.cs, add OpenAPI generation:
   builder.Services.AddOpenApi(options =>
   {
       options.AddDocumentTransformer((document, context, ct) =>
       {
           document.Info.Title = "ReleasePilot API";
           document.Info.Version = "v1";
           document.Info.Description = "Promotion engine for application lifecycle management";
           return Task.CompletedTask;
       });
   });

2. Map the OpenAPI document and Scalar UI:
   app.MapOpenApi();
   app.MapScalarApiReference(options =>
   {
       options.Title = "ReleasePilot";
       options.Theme = ScalarTheme.Purple;
       options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
   });

3. Add WithOpenApi() to every endpoint group so all routes appear in the docs.

4. Add metadata to each endpoint:
   - .WithName("RequestPromotion")
   - .WithSummary("Request a new promotion")
   - .WithDescription("Moves an application version one step forward in the pipeline")
   - .WithTags("Promotions") or .WithTags("Applications") as appropriate
   - .Produces<T>(200) and .ProducesProblem(422) etc. for each endpoint

5. Verify Scalar UI is accessible at /scalar/v1 when running the API.
```

---

### Prompt 5 — Health checks

```
In ReleasePilot.Api, add health checks for PostgreSQL and RabbitMQ.

1. Add these packages to Directory.Packages.props and to ReleasePilot.Api:
   - AspNetCore.HealthChecks.Npgsql
   - AspNetCore.HealthChecks.Rabbitmq

2. Register health checks in Program.cs:
   builder.Services.AddHealthChecks()
       .AddNpgsql(
           connectionString: configuration.GetConnectionString("DefaultConnection"),
           name: "postgresql",
           tags: ["db", "ready"])
       .AddRabbitMQ(
           rabbitConnectionString: $"amqp://{config["RabbitMq:Username"]}:{config["RabbitMq:Password"]}@{config["RabbitMq:Host"]}",
           name: "rabbitmq",
           tags: ["messaging", "ready"]);

3. Map two health check endpoints:
   app.MapHealthChecks("/health/live", new HealthCheckOptions
   {
       Predicate = _ => false
   });

   app.MapHealthChecks("/health/ready", new HealthCheckOptions
   {
       Predicate = check => check.Tags.Contains("ready"),
       ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
   });

   /health/live  → liveness probe (always 200 if app is running)
   /health/ready → readiness probe (checks db and rabbitmq)

4. Add these packages to Directory.Packages.props:
   - AspNetCore.HealthChecks.UI.Client (for UIResponseWriter)
```

---

### Prompt 6 — Command endpoint tests

```
In tests/, create a new xUnit project ReleasePilot.Tests.Api.
Add it to All.sln and add project references to:
  - ReleasePilot.Domain
  - ReleasePilot.Application
  - ReleasePilot.Infrastructure
  - ReleasePilot.Api

Add these packages to the project:
  - Microsoft.AspNetCore.Mvc.Testing
  - Testcontainers.PostgreSql
  - Testcontainers.RabbitMq

Create a shared WebApiFactory in Tests.Api/Infrastructure/:

  class WebApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
  - Overrides ConfigureWebHost to replace the real DB and RabbitMQ
    connection strings with Testcontainers instances
  - Starts PostgreSqlContainer and RabbitMqContainer in InitializeAsync
  - Applies EF Core migrations in InitializeAsync
  - Stops containers in DisposeAsync

Tests to write in PromotionCommandEndpointTests:

  POST /promotions — happy path
  - Send valid body, assert 201 Created
  - Assert Location header contains /promotions/{id}
  - Assert response body contains a valid Guid as id

  POST /promotions/{id}/approve — happy path
  - Create a promotion first, then approve it
  - Assert 200 OK with status "Approved"

  POST /promotions — invalid body (empty applicationId)
  - Assert 400 Bad Request
  - Assert response is ProblemDetails with validation errors

  POST /promotions — environment skipped
  - Try to request a Production promotion without completing Dev and Staging
  - Assert 422 Unprocessable Entity
  - Assert ProblemDetails detail mentions the skipped environment

  POST /promotions/{id}/approve — non-approver user
  - Send approverRoles: [] (empty)
  - Assert 403 Forbidden

  POST /promotions/{id}/cancel — already completed
  - Complete a promotion, then try to cancel it
  - Assert 409 Conflict
```

---

### Prompt 7 — Query endpoint tests

```
In ReleasePilot.Tests.Api, write tests for the query endpoints.
Reuse WebApiFactory from Step 6.

Tests to write in PromotionQueryEndpointTests:

  GET /promotions/{id} — existing promotion
  - Create a promotion via POST /promotions
  - Call GET /promotions/{id}
  - Assert 200 OK
  - Assert response body contains id, status, targetEnvironment, version
  - Assert history array contains at least one transition

  GET /promotions/{id} — not found
  - Call GET /promotions/{Guid.NewGuid()}
  - Assert 404 Not Found
  - Assert response is ProblemDetails

  GET /applications/{id}/status — no promotions
  - Call with a random applicationId that has no promotions
  - Assert 200 OK
  - Assert all environments show null for lastCompletedVersion and activePromotion

  GET /applications/{id}/status — with active promotion
  - Create a promotion for Dev environment
  - Call GET /applications/{id}/status
  - Assert Dev environment shows the active promotion
  - Assert Staging and Production show null

  GET /applications/{id}/promotions — pagination
  - Create 3 promotions for the same applicationId
  - Call with page=1&pageSize=2
  - Assert items count is 2
  - Assert totalCount is 3
  - Assert totalPages is 2

  GET /applications/{id}/promotions — empty history
  - Call with a random applicationId
  - Assert 200 OK with empty items array and totalCount 0
```

---

### Prompt 8 — Exception middleware tests

```
In ReleasePilot.Tests.Api, write focused tests for the exception middleware.
Reuse WebApiFactory from Step 6.

Tests to write in DomainExceptionMiddlewareTests:

  DomainException maps to 422
  - Trigger EnvironmentSkippedException via a real HTTP request
    (request Production promotion without completing Dev)
  - Assert status code is 422
  - Assert Content-Type is application/problem+json
  - Assert response body has "type", "title", "status", "detail" fields
  - Assert "status" field value is 422

  ConcurrentPromotionException maps to 409
  - Create an active promotion for App X on Dev
  - Try to create a second promotion for App X on Dev
  - Assert 409 Conflict

  ImmutablePromotionException maps to 409
  - Complete a promotion
  - Try to approve it again
  - Assert 409 Conflict

  ValidationException maps to 400
  - Send a command with missing required fields
  - Assert 400 Bad Request
  - Assert response body contains "errors" with field-level details

  UnauthorizedApprovalException maps to 403
  - Try to approve with empty roles
  - Assert 403 Forbidden

  Unknown exception maps to 500
  - This requires temporarily injecting a broken handler via
    WebApplicationFactory.WithWebHostBuilder overrides
  - Assert 500 Internal Server Error
  - Assert response body does NOT contain a stack trace
  - Assert response body contains a generic error message only
```

---

### Prompt 9 — End-to-end smoke test

```
Create a manual smoke test script that exercises the full system
running against the real docker-compose infrastructure.

1. Create a file tests/smoke/smoke-test.http usable with the
   VS Code REST Client extension or JetBrains HTTP Client.
   Include requests for every command and query in order:

   ### 1. Request promotion to Dev
   POST http://localhost:5000/promotions
   Content-Type: application/json
   { "applicationId": "...", "version": "1.0.0",
     "targetEnvironment": "Dev", "requestedByUserId": "..." }

   ### 2. Approve promotion
   POST http://localhost:5000/promotions/{{promotionId}}/approve
   Content-Type: application/json
   { "approverId": "...", "approverRoles": ["approver"] }

   ### 3. Start deployment
   ### 4. Complete promotion
   ### 5. Request promotion to Staging (same version)
   ### 6. Approve, Start, Complete for Staging
   ### 7. Request promotion to Production
   ### 8. Get promotion detail
   ### 9. Get application status (should show all 3 environments)
   ### 10. Get promotion history

2. Add a smoke-test checklist to the README:
   - docker compose up -d
   - dotnet ef database update --project src/ReleasePilot.Infrastructure \
       --startup-project src/ReleasePilot.Api
   - dotnet run --project src/ReleasePilot.Api
   - Open http://localhost:5000/health/ready — should return Healthy
   - Open http://localhost:5000/scalar/v1 — Scalar UI should load
   - Run smoke-test.http top to bottom — all requests should succeed
   - Check RabbitMQ management UI at http://localhost:15672
     (guest/guest) — audit-log queue should show consumed messages
   - Query the audit_log table directly in PostgreSQL —
     should contain one entry per domain event fired

3. Run dotnet test All.sln one final time.
   All tests across all projects must be green before moving to Phase 4.
```

---

## Phase 4 — Delivery

### Prompt 1 — README

```
Write a complete README.md for the ReleasePilot project at the solution root.
The evaluator must be able to get the system running without asking any questions.

Structure the README with these sections:

---

## Overview
Brief description of what ReleasePilot is and what it demonstrates:
DDD, CQRS, domain events, ports & adapters, async messaging with MassTransit.

---

## Architecture
Short explanation of the four layers and why they are structured this way:
- Domain: pure business logic, no dependencies
- Application: CQRS handlers, port interfaces, orchestration
- Infrastructure: EF Core, MassTransit, in-memory stubs
- Api: Minimal API endpoints, exception middleware

Include a note on where port interfaces live and why (Application, not Domain).

---

## Prerequisites
- .NET 10 SDK
- Docker and Docker Compose
- dotnet-ef tool: dotnet tool install --global dotnet-ef

---

## Getting started

Step by step:

1. Clone the repo
2. Start infrastructure:
     docker compose up -d
3. Apply migrations:
     dotnet ef database update \
       --project src/ReleasePilot.Infrastructure \
       --startup-project src/ReleasePilot.Api
4. Run the API:
     dotnet run --project src/ReleasePilot.Api
5. Verify:
     http://localhost:5000/health/ready   → should return Healthy
     http://localhost:5000/scalar/v1      → Scalar UI

---

## Example requests

One curl example per command and query, in logical order:

1. Request a promotion to Dev
2. Approve the promotion
3. Start deployment
4. Complete the promotion
5. Request a promotion to Staging (same version, Dev must be completed first)
6. Get promotion detail
7. Get application status
8. Get promotion history (paginated)
9. Attempt to skip an environment (expected 422 response)
10. Attempt to approve as non-approver (expected 403 response)

Use realistic UUIDs in the examples. Show both the request and
the expected response shape for each one.

---

## Running the tests
  dotnet test All.sln --verbosity normal

---

## Design decisions
Short bullet list of the most important trade-offs made:
- Why port interfaces live in Application and not Domain
- Why persistence entities are separate from the domain aggregate
- Why domain events are published after the repository saves (not before)
- What you would improve with more time

---

Keep the README concise. No marketing language.
Every command shown must actually work against the real codebase.
```
