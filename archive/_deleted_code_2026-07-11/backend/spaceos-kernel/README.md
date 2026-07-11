# SpaceOS.Kernel

Clean Architecture · DDD · CQRS · .NET 8 LTS

A production-ready REST API for managing multi-tenant space operations — tenants, facilities, work stations, space layers, flow epics, and an append-only audit log.

---

## Architecture

```
SpaceOS.Kernel.Domain           ← aggregates, value objects, domain events, repository interfaces
SpaceOS.Kernel.Application      ← CQRS handlers (MediatR), validators, DTOs, audit dispatch
SpaceOS.Infrastructure          ← EF Core 8 + PostgreSQL (Npgsql 8.0.11), repository implementations
SpaceOS.Kernel.Api              ← ASP.NET Core Minimal API, JWT auth, rate limiting, OpenAPI
SpaceOS.Kernel.Tests            ← xUnit v3, Moq — unit tests (~214)
SpaceOS.Kernel.IntegrationTests ← xUnit v3 — repository + pipeline integration tests (~92)
SpaceOS.Kernel.Api.Tests        ← xUnit v3, WebApplicationFactory — API integration tests (~44)
```

**Layer dependency rule:** `Domain ← Application ← Infrastructure ← Api`

---

## Quick Start (Docker)

**Prerequisites:** Docker, Docker Compose

```bash
# 1. Set required secrets
export POSTGRES_PASSWORD=your_strong_password
export JWT_SIGNING_KEY=your_32_char_minimum_signing_key

# 2. Start API + PostgreSQL
docker-compose up --build
```

- API: `http://localhost:5000`
- Health check: `http://localhost:5000/healthz`
- Swagger UI: `http://localhost:5000/openapi/v1.json` *(only in Development)*

```bash
# Stop (keep data)
docker-compose down

# Stop and remove data volume
docker-compose down -v
```

---

## Local Development

**Prerequisites:** .NET 8 SDK, PostgreSQL 15+

### 1. Configure the database

Set the connection string — either in `SpaceOS.Kernel.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=spaceos;Username=spaceos;Password=your_password"
  },
  "Jwt": {
    "SigningKey": "your_32_char_minimum_signing_key",
    "Issuer": "spaceos-kernel",
    "Audience": "spaceos-kernel-api"
  }
}
```

Or via environment variables:

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;..."
export Jwt__SigningKey="your_32_char_minimum_signing_key"
```

### 2. Apply migrations

```bash
dotnet ef database update \
  --project SpaceOS.Infrastructure \
  --startup-project SpaceOS.Kernel.Api
```

> **Never** call `Database.Migrate()` at startup — migrations are applied via CLI only.

### 3. Run the API

```bash
dotnet run --project SpaceOS.Kernel.Api
```

---

## Configuration

All sensitive values must be provided via environment variables. Never commit real secrets.

| Variable | Description | Required |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Full PostgreSQL connection string | Yes |
| `Jwt__SigningKey` | HMAC-SHA256 signing key (≥ 32 chars) | Yes |
| `Jwt__Issuer` | JWT issuer claim | No (default: `spaceos-kernel`) |
| `Jwt__Audience` | JWT audience claim | No (default: `spaceos-kernel-api`) |
| `ASPNETCORE_ENVIRONMENT` | `Development` / `Production` | No (default: `Production`) |
| `ASPNETCORE_URLS` | API listen address | No (default: `http://+:8080`) |
| `POSTGRES_PASSWORD` | PostgreSQL password (Docker Compose) | Yes (Docker) |
| `JWT_SIGNING_KEY` | JWT signing key (Docker Compose) | Yes (Docker) |

---

## API Endpoints

All endpoints require a valid **JWT Bearer token** (`Authorization: Bearer <token>`).

| Route | Methods | Auth policy |
|---|---|---|
| `/api/tenants` | GET (list, by id), POST, PUT | ReadPolicy / AdminPolicy |
| `/api/facilities` | GET (list, by id, by tenant), POST, PUT | ReadPolicy / WritePolicy / AdminPolicy |
| `/api/work-stations` | GET (by id, by facility), POST, PUT (status, reassign) | ReadPolicy / WritePolicy |
| `/api/space-layers` | GET (by id, by facility), POST, PUT (intent) | ReadPolicy / WritePolicy |
| `/api/flow-epics` | GET (by id, by facility), POST, PUT (title, delegate, start) | ReadPolicy / WritePolicy |
| `/api/audit-events` | GET (paged, filtered by tenantId + date range) | RequireAuthorization |
| `/api/auth/token` | POST — issue JWT token (dev/demo) | — |
| `/healthz` | GET — DB connectivity check | — |

### RBAC Roles

| Role | Policies |
|---|---|
| `Joiner` | ReadPolicy |
| `Designer` | ReadPolicy, WritePolicy |
| `Admin` | ReadPolicy, WritePolicy, AdminPolicy |

### Rate Limiting

| Policy | Limit | Applied to |
|---|---|---|
| `fixed` | 100 req / 60s per IP | All GET endpoints |
| `sliding` | 20 req / 60s per IP | All POST/PUT endpoints |

Rate limit exceeded → `429 Too Many Requests` with `Retry-After: 60` header.

---

## Testing

```bash
# All tests
dotnet test

# Unit tests only
dotnet test SpaceOS.Kernel.Tests

# Repository + pipeline integration tests
dotnet test SpaceOS.Kernel.IntegrationTests

# API integration tests
dotnet test SpaceOS.Kernel.Api.Tests
```

**350 tests, 0 failures** across unit, integration, and API layers.

---

## Database Migrations

```bash
# Apply all pending migrations
dotnet ef database update \
  --project SpaceOS.Infrastructure \
  --startup-project SpaceOS.Kernel.Api

# Create a new migration
dotnet ef migrations add <MigrationName> \
  --project SpaceOS.Infrastructure \
  --startup-project SpaceOS.Kernel.Api \
  --output-dir Migrations

# Roll back last migration
dotnet ef migrations remove \
  --project SpaceOS.Infrastructure \
  --startup-project SpaceOS.Kernel.Api
```

---

## Docker Details

- **Base image:** `mcr.microsoft.com/dotnet/aspnet:8.0-alpine`
- **Image size:** 182MB
- **Non-root user:** `appuser`
- **Health check:** `GET /healthz` every 30s

```bash
# Build image manually
docker build -t spaceos-api .

# Run standalone (requires external PostgreSQL)
docker run --rm \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;..." \
  -e Jwt__SigningKey="your_key" \
  -p 5000:8080 \
  spaceos-api
```
