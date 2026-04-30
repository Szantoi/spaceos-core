---
id: MSG-PARTNER-001
from: root
to: partner
type: task
priority: high
status: READ
ref: SpaceOS_PartnerTier_Architecture_v1.md
created: 2026-04-27
---

# PARTNER-001 — Inbox #1: Scaffold + Domain + API Key Auth (Nap 1–3)

> **Tervdok:** `docs/architecture/SpaceOS_PartnerTier_Architecture_v1.md` — KÖTELEZŐ olvasmány!
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Repo:** `/opt/spaceos/spaceos-partner-api/`
> **Használhatsz sub-agent-eket** ha szükséges

---

## Nap 1 — Scaffold

Hasonló struktúra mint a FreeTier API:

```bash
dotnet new sln -n SpaceOS.PartnerTier
```

Projektek:
- `src/SpaceOS.PartnerTier.Domain/` — aggregates, VOs, events
- `src/SpaceOS.PartnerTier.Application/` — commands, queries, handlers
- `src/SpaceOS.PartnerTier.Infrastructure/` — EF Core, Redis, adapters
- `src/SpaceOS.PartnerTier.Api/` — Minimal API, middleware
- `tests/SpaceOS.PartnerTier.Tests/`

`Directory.Build.props`: net8.0, Nullable, TreatWarningsAsErrors
`Program.cs`: /healthz endpoint, Kestrel 127.0.0.1:5011

---

## Nap 2 — Partner domain model

**Partner aggregate** (spec §3):
```csharp
public sealed class Partner
{
    public Guid Id { get; }
    public string CompanyName { get; }
    public string ContactEmail { get; }
    public PartnerStatus Status { get; }  // PendingReview → Active → Suspended → Terminated
    public PartnerTier Tier { get; }      // Free, Pro, Enterprise
    public string? CorsAllowlist { get; } // comma-separated origins
    public string? BrandingJson { get; }  // partner branding config
    
    public static Result<Partner> Register(string companyName, string contactEmail) { }
    public Result Activate() { }
    public Result Suspend(string reason) { }
}
```

**PartnerApiKey entity:**
```csharp
public sealed class PartnerApiKey
{
    public Guid Id { get; }
    public Guid PartnerId { get; }
    public string KeyHash { get; }      // SHA-256 hash (NEM plaintext!)
    public string KeyPrefix { get; }    // első 8 karakter (azonosításhoz)
    public bool IsActive { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? RevokedAt { get; }
    
    public static (PartnerApiKey key, string rawKey) Generate(Guid partnerId) { }
}
```

**Enums:** PartnerStatus (4), PartnerTier (3)
**Domain events:** PartnerRegistered, Activated, Suspended, ApiKeyGenerated, ApiKeyRevoked

---

## Nap 3 — API Key Auth middleware

```csharp
// Api/Middleware/ApiKeyAuthMiddleware.cs
// Header: X-Api-Key: {raw_key}
// Flow:
// 1. KeyPrefix extract (első 8 char)
// 2. DB lookup by KeyPrefix
// 3. SHA-256 hash compare (FixedTimeEquals!)
// 4. Partner aktív? Tier?
// 5. HttpContext.Items["PartnerId"] = partner.Id
// 6. Ha invalid → 401
```

**Rate limit middleware:**
```csharp
// Per-partner bucket (Redis):
// Free: 5/perc
// Pro: 50/perc
// Enterprise: custom
```

---

## Tesztek (30+)

- Partner: Register, Activate, Suspend, FSM transitions
- ApiKey: Generate, hash verify (FixedTimeEquals), revoke, max 3 active
- Auth middleware: valid key → 200, invalid → 401, revoked → 401
- Rate limit: over limit → 429
- /healthz → 200

## Definition of Done

- [ ] Solution scaffold (5 projekt)
- [ ] Partner aggregate + PartnerApiKey
- [ ] API Key auth middleware (SHA-256, FixedTimeEquals)
- [ ] Rate limit middleware (Redis, per-partner)
- [ ] /healthz → 200
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 30 pass
- [ ] Outbox DONE
