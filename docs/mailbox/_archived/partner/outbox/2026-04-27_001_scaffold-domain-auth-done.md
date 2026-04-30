---
id: partner-outbox-001
from: partner
to: root
type: done
priority: high
status: READ
ref_inbox: 2026-04-27_001_partner-inbox1-scaffold-domain-auth.md
date: 2026-04-27
---

# DONE: SpaceOS PartnerTier — Scaffold + Domain + API Key Auth

## Eredmény

- `dotnet build`: 0 error, 0 warning
- `dotnet test`: 56 pass, 0 fail, 0 skip

## Megvalósított

### Solution struktúra (5 projekt)
```
SpaceOS.PartnerTier.sln
Directory.Build.props          ← net8.0, Nullable=enable, TreatWarningsAsErrors=true
src/
  SpaceOS.PartnerTier.Domain/
  SpaceOS.PartnerTier.Application/
  SpaceOS.PartnerTier.Infrastructure/
  SpaceOS.PartnerTier.Api/
tests/
  SpaceOS.PartnerTier.Tests/
```

### Domain model
- `PartnerStatus` és `PartnerTier` enumok
- `Partner` aggregate FSM: Register → PendingReview → Active → Suspended
- `PartnerApiKey` entity: SHA-256 hash, FixedTimeEquals, DisplayHint (last 4 chars)
- `Result<T>` pattern (külső library nélkül)
- Domain events: PartnerRegistered, PartnerActivated, PartnerSuspended, ApiKeyGenerated, ApiKeyRevoked

### Infrastructure
- `PartnerDbContext` EF Core 8.0.11 + Npgsql 8.0.11
- `PartnerConfiguration` + `PartnerApiKeyConfiguration` (IEntityTypeConfiguration)
- schema: `spaceos_partner`, KeyHash: `bytea`

### API
- Program.cs: Swashbuckle, MediatR, Redis (lazy singleton factory), Kestrel loopback:5011
- `PartnerApiKeyMiddleware`: X-Partner-Api-Key header, SHA-256 + FixedTimeEquals, /healthz + /swagger skip
- `PartnerRateLimitMiddleware`: Redis counter, Free=5/min, Pro=50/min, Enterprise=200/min
- `/healthz` → 200 `{ status: "healthy", service: "spaceos-partner-api" }`

### Tesztek (56 db)
- `PartnerTests`: 17 teszt — Register, FSM transitions, domain events
- `PartnerApiKeyTests`: 13 teszt — Generate, VerifyRawKey, Revoke, UpdateLastUsed
- `ResultTests`: 4 teszt
- `ApiKeyMiddlewareTests`: 8 teszt — in-memory EF Core, valid/invalid/revoked key, path skip
- `RateLimitMiddlewareTests`: 9 teszt — Moq Redis, Free/Pro/Enterprise tier limits
- `HealthzTests`: 4 teszt — WebApplicationFactory, in-memory DB + Moq Redis

## Technikai megjegyzések

1. **Namespace conflict**: `PartnerTier` enum neve ütközött a `SpaceOS.PartnerTier` namespace-szel — `using DomainTier = SpaceOS.PartnerTier.Domain.Enums.PartnerTier` alias megoldja
2. **Npgsql verzió**: 8.0.14 nem volt cache-ben, 8.0.11-et használ (kompatibilis)
3. **AddOpenApi/MapOpenApi**: .NET 9 API, .NET 8-ban Swashbuckle 6.6.2-t használ helyette
4. **Redis lazy init**: `ConnectionMultiplexer.Connect()` factory delegate-be kerül hogy a WebApplicationFactory tesztekben a mock felülírhassa
5. **Kestrel**: `IsEnvironment("Testing")` check — tesztekben nem köti le a 5011-es portot

## Következő lépés (Inbox #2)
- Embed token (signed JWT, 1h TTL)
- Lead capture (AES-256 encrypted email)
- Commission tracking
- Admin Portal endpoints
