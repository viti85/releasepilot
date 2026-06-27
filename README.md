# ReleasePilot

ReleasePilot is a state-of-the-art deployment promotion engine designed for managing application lifecycles across different environments (`Dev`, `Staging`, `Production`). It ensures structural integrity and validation checkpoints (e.g., approvals, deployment progress, and sequential environment compliance) before promoting application versions.

---

## Technical Stack & Architecture

- **Domain-Driven Design (DDD)**: Expresses domain invariants, states, and sequential pipeline transitions cleanly.
- **ASP.NET Core Minimal APIs**: Clean, high-performance command and query endpoints.
- **MediatR (CQRS)**: Separates writes (Commands) from reads (Queries) cleanly.
- **Entity Framework Core & PostgreSQL**: Reconstitutes promotion aggregate state history and stores audit logs.
- **MassTransit & RabbitMQ**: Publishes integration events to process background audit logging asynchronously.
- **Scalar**: Modern, visual API Reference playground.

---

## Smoke Test Checklist

Follow these steps to spin up the real infrastructure, run the application, and test the endpoints:

### 1. Start Infrastructure
Run Docker Compose from the root directory to launch PostgreSQL and RabbitMQ:
```bash
docker compose up -d
```

### 2. Run Database Migrations
Apply the EF Core migrations to prepare the PostgreSQL tables (`promotions`, `promotion_state_transitions`, and `audit_log`):
```bash
dotnet ef database update --project src/ReleasePilot.Infrastructure --startup-project src/ReleasePilot.Api
```

### 3. Start the Web API
Run the ASP.NET Core API project locally:
```bash
dotnet run --project src/ReleasePilot.Api
```
The application will listen at `http://localhost:5000` (HTTP) and `https://localhost:5001` (HTTPS).

### 4. Verify Health Checks
Open the readiness probe in your browser:
- [Readiness Probe](http://localhost:5000/health/ready)
- Expect a `Healthy` status (HTTP 200 OK) response showing database and RabbitMQ checks are fully operational.

### 5. Verify API Documentation & UI
Load the Scalar API Reference UI in your browser to browse and interact with the endpoints:
- [Scalar API Docs](http://localhost:5000/scalar/v1)

### 6. Execute the Smoke Test Script
Open the HTTP client test file:
- [`tests/smoke/smoke-test.http`](file:///c:/Code/releasepilot/tests/smoke/smoke-test.http)
- Run the requests top-to-bottom sequentially. All requests should complete successfully with expected HTTP response statuses (201, 200).

### 7. Inspect Messaging and Database Auditing
- **RabbitMQ**: Visit the [RabbitMQ Management UI](http://localhost:15672) (credentials: `guest` / `guest`). Check the `audit-log` queue to verify that the message consumer has processed the domain events successfully.
- **PostgreSQL**: Connect to the database and query the `audit_log` table. You should find a serialized JSON entry matching every domain event that was fired during the promotion lifecycle transitions.
