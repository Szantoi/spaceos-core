---
id: MSG-FREETIER-001
from: root
to: freetier
type: task
priority: high
status: READ
ref: MSG-ARCH-005-RESPONSE
created: 2026-04-20
---

# FREETIER-001 — Repo scaffold + domain alapréteg (Nap 1.0–2.5)

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Spec:** `docs/architecture/SpaceOS_FreeTier_Architecture_v4.md` — **TELJES DOKUMENTUMOT OLVASD EL** session elején
> **Timeline:** ~2.5 nap (Nap 1.0..2.5 — Section 10 ütemterv)

---

## Kötelező olvasnivalók session indításakor

1. `docs/architecture/SpaceOS_FreeTier_Architecture_v4.md` — teljes spec (gate-check: Section 10)
2. `docs/knowledge/patterns/DATABASE_PATTERNS.md` — RLS + GUC + EF migration
3. `docs/knowledge/patterns/DEV_DIFFICULTIES.md` — visszatérő csapdák
4. `docs/knowledge/security/SECURITY_PATTERNS.md` — JWT/RBAC, token hashing minták

---

## Repo helye

```
/opt/spaceos/spaceos-freetier-api/
```

Ha a mappa nem létezik, hozd létre és scaffoldold (`mkdir -p`, `dotnet new sln`, stb.)

---

## Nap 1.0 — Repo scaffold

```
spaceos-freetier-api/
  SpaceOS.FreeTier.sln
  src/
    SpaceOS.FreeTier.Domain/          (.NET 8 classlib)
    SpaceOS.FreeTier.Application/     (.NET 8 classlib)
    SpaceOS.FreeTier.Infrastructure/  (.NET 8 classlib)
    SpaceOS.FreeTier.Api/             (.NET 8 minimal API)
  tests/
    SpaceOS.FreeTier.Domain.Tests/    (.NET 8 xUnit)
    SpaceOS.FreeTier.Application.Tests/
    SpaceOS.FreeTier.Integration.Tests/
  Directory.Build.props
  CLAUDE.md                           ← freetier terminál instrukciók
  .gitignore
```

**NuGet csomagok (Directory.Build.props vagy csproj-onként):**
- `MediatR` 12.x
- `FluentValidation` 11.x
- `Ardalis.Result`
- `Npgsql.EntityFrameworkCore.PostgreSQL` 8.x
- `StackExchange.Redis`
- `QuestPDF`
- `Serilog.AspNetCore`
- `Microsoft.AspNetCore.OpenApi`
- `xunit`, `FluentAssertions`, `NSubstitute`, `Testcontainers.PostgreSql`, `Testcontainers.Redis`

**Contracts NuGet:** `SpaceOS.Modules.Contracts` v1.3.0 (local source: `/opt/spaceos/spaceos-modules-contracts/artifacts`)

---

## Nap 1.5 — API skeleton

**Fájl:** `SpaceOS.FreeTier.Api/Program.cs`

```csharp
// Minimal API skeleton
// /healthz endpoint → {"status":"healthy"}
// Serilog + appsettings.json
// Port: 5010
```

`appsettings.json`:
```json
{
  "ConnectionStrings": {
    "FreeTier": "<from env>",
    "Redis": "<from env>"
  },
  "Kestrel": { "Endpoints": { "Http": { "Url": "http://127.0.0.1:5010" } } }
}
```

**Tesztek (+2):** `GET /healthz` → 200, response `{"status":"healthy"}`

---

## Nap 2.0 — Domain aggregates

**Spec:** Section 3 (Domain Model), D-26 (IClock)

Három aggregate (`FreeTierUser`, `Workspace`, `UpgradeRequest`) + `IDomainEventContainer` + `IClock`:

```csharp
// Domain/Abstractions/IClock.cs
public interface IClock { DateTimeOffset UtcNow { get; } }

// Domain/Abstractions/IDomainEventContainer.cs
public interface IDomainEventContainer
{
    IReadOnlyList<IDomainEvent> PopDomainEvents();
    void RaiseDomainEvent(IDomainEvent evt);
}

// Domain/Aggregates/FreeTierUser.cs
public sealed class FreeTierUser : AggregateRoot<Guid>, IDomainEventContainer
{
    public string Email { get; private set; }
    public DateTimeOffset? AuthenticatedAt { get; private set; }  // SEC-15, D-11-REV
    public int WorkspaceCount { get; private set; }
    // ...
}

// Domain/Aggregates/Workspace.cs — max 20/user (D-23)
// Domain/Aggregates/UpgradeRequest.cs — state machine
```

**Tesztek (+25):** aggregate factory methods, FSM átmenetek, domain event firing, IClock mock

---

## Nap 2.5 — Domain entities + value objects

**Spec:** Section 3.2

```
Domain/Entities/
  WorkspaceRevision.cs
  ShareToken.cs              ← TokenHash VARCHAR(64) + TokenPrefix VARCHAR(8) (D-13-REV)
  MagicLinkToken.cs          ← SHA-256 hash (D-11-REV), max 3 pending/user, FixedTimeEquals

Domain/ValueObjects/
  NestingInput.cs            ← JSONB backing (D-21)
  SessionNonce.cs            ← 16 byte CSPRNG (D-14-REV)

Domain/Enums/
  UpgradeRequestStatus.cs    ← Draft/Submitted/UnderReview/Accepted/Rejected
  WorkspaceStatus.cs

Domain/Events/               ← 7 domain event (spec Section 5)
  UserRegistered.cs
  MagicLinkRequested.cs
  MagicLinkVerified.cs
  WorkspaceSaved.cs
  ShareTokenGenerated.cs
  ShareTokenRevoked.cs
  UpgradeRequested.cs
```

**Tesztek (+15):** entity invariánsok, VO validáció, token hash ellenőrzés

---

## Definition of Done

- [ ] Repo scaffold: sln + 6 csproj + NuGet + `Directory.Build.props`
- [ ] `/healthz` endpoint + 2 teszt
- [ ] `FreeTierUser`, `Workspace`, `UpgradeRequest` aggregates + `IClock` + `IDomainEventContainer`
- [ ] `WorkspaceRevision`, `ShareToken` (TokenHash/Prefix), `MagicLinkToken` entities
- [ ] Value objects: `NestingInput`, `SessionNonce`
- [ ] 7 domain event class
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥42 pass
- [ ] Outbox DONE üzenet küldve

---

## Megjegyzés: párhuzamos INFRA task

Az INFRA terminál párhuzamosan telepíti a Redis-t, nginx vhost-ot és a DB-t. A Nap 3.5 (migration) előtt az INFRA DONE-ra szükség van — addig a domain + application rétegen dolgozhatsz.
