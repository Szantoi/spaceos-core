# SpaceOS.Modules.Sales — CLAUDE.md

## SESSION STARTUP/SHUTDOWN RITUAL

**Minden session elején:**
```bash
# 0. Datahaven státusz regisztráció — jelezd hogy dolgozol
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "sales",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'

# 1. Inbox ellenőrzés
ls /opt/spaceos/docs/mailbox/sales/inbox/
grep -l "status: UNREAD" /opt/spaceos/docs/mailbox/sales/inbox/*.md 2>/dev/null
```

**Session végén (DONE/BLOCKED outbox után):**
```bash
# Datahaven státusz regisztráció — jelezd hogy befejeztél
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{\"terminal\":\"sales\",\"status\":\"idle\"}'
```

**Datahaven Dashboard:** https://datahaven.joinerytech.hu (token: `dev-token-spaceos-dashboard-2026`)
- Dashboard (`/`) — Sales státusz (WORKING/IDLE), inbox/outbox metrikák
- Kanban (`/kanban`) — Sales swimlane a Delivery track-en
- Teljes API: `docs/WORKFLOW.md` — "Datahaven Dashboard" szakasz

---

## JELENLEGI ÁLLAPOT (2026-05-28)

| | |
|---|---|
| **Terminál** | sales · Port: **5009** · Mailbox: `/opt/spaceos/docs/mailbox/sales/` |
| **Aktuális commit** | — (új polyrepo, nincs még commit) |
| **Tesztek** | — (0, implementáció kezdődik) |
| **VPS** | NOT DEPLOYED |

### Feladat
**MSG-SALES-001** — Sales modul implementáció, teljes spec: `docs/tasks/active/SpaceOS_Modules_Sales_Architecture_v4.md`

### Blokkolók státusza (ADR-039)
- ✅ `GET /api/internal/tenants/{id}` — Kernel, commit `c70a359`
- ✅ `POST /joinery/internal/orders/from-quote` — Joinery, commit `da7199f`
- ⏳ `SpaceOS:InternalSecret` env var — INFRA deploy folyamatban (adapter tesztek stub-fixture-rel futtathatók)

---

## Stack
- .NET 8, Clean Architecture + DDD + CQRS + Outbox pattern
- PostgreSQL 16 schema: `spaceos_sales` (önálló schema)
- EF Core 8 + Npgsql 8.0.11
- Port: **5009** (loopback-only, systemd)

## Repo bootstrap — sablon
```bash
# Cutting solution struktúrája a minta:
ls /opt/spaceos/backend/spaceos-modules-cutting/
# NuGet feed: /opt/spaceos/backend/local-nuget/ (GitHub Packages helyi mirror)
cat /opt/spaceos/backend/spaceos-modules-cutting/NuGet.Config
```

## Approved packages
MediatR 12.4.1 · FluentValidation 12.1.1 · Ardalis.Result 10.1.0 · Ardalis.Specification 8.0.0
EF Core 8.0.11 · Npgsql 8.0.11 · Polly 8.x · xUnit v3 · Moq 4.20.72 · FluentAssertions 6.12.2

Adding anything outside this list requires explicit discussion first.

## Solution structure (5 projekt + Tests)
```
SpaceOS.Modules.Sales.Domain          ← aggregates (Quote, SalesOrder), VOs, events, repo interfaces
SpaceOS.Modules.Sales.Application     ← CQRS handlers, validators, specs, ITenantContext
SpaceOS.Modules.Sales.Abstractions    ← IOrderConversionPort, IKernelActorDirectoryPort (Contracts NuGet-ba is kerül)
SpaceOS.Modules.Sales.Infrastructure  ← EF Core, migrations, adapters, OutboxWorker
SpaceOS.Modules.Sales.Api             ← Minimal API endpoints, Program.cs
SpaceOS.Modules.Sales.Tests           ← xUnit v3, Testcontainers, Moq
```

## Layer dependency rule (hard constraint)
```
Domain ← Application ← Infrastructure ← Api
                                       ← Tests
```
Domain has zero external NuGet dependencies (except Ardalis.Result).

## Track sorrend (§13 spec alapján)
```
A → B → C → D → E → F → G (tesztek folyamatosan, minden track-kel párhuzamosan)
```
Részletek: `docs/tasks/active/SpaceOS_Modules_Sales_Architecture_v4.md` §13

## Cross-module hívások (loopback)
```
POST http://127.0.0.1:5002/joinery/internal/orders/from-quote
  Header: X-SpaceOS-Internal: {SpaceOS:InternalSecret}
  Header: X-SpaceOS-TenantId: {tenantId}

GET  http://127.0.0.1:5000/api/internal/tenants/{id}
  Header: X-SpaceOS-Internal: {SpaceOS:InternalSecret}
  Header: X-SpaceOS-TenantId: {callerTenantId}
```

## PostgreSQL: spaceos_sales schema
- `Quotes`, `QuoteLines`: immutable + RLS FORCE + `xmin` rowversion
- `SalesOrders`, `SalesOrderLines`: RLS FORCE
- `OutboxMessages`: tenant-aware, `MarkInFlight` / `MarkCompleted` / `RecordFailure`
- Minden tábla: `app.current_tenant_id` GUC key (NEM `app.tenant_id` — az joinery-specifikus)

## Naming conventions
| Scope | Convention |
|---|---|
| Classes, methods, properties | PascalCase |
| Private fields | _camelCase |
| Local variables | camelCase |
| CancellationToken param | always `ct` |
| File name | 1:1 with class name |

## Universal code rules
```csharp
// 1. ConfigureAwait(false) minden production async callban
await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);

// 2. CancellationToken neve mindig ct
public async Task<Result<T>> Handle(TRequest request, CancellationToken ct)

// 3. AsNoTracking() minden read-only lekérdezésnél
_db.Quotes.AsNoTracking().Where(...)

// 4. Result<T> minden handler return type
public async Task<Result<QuoteResponse>> Handle(...)
```

## Golden Rules
1. Nincs public setter az aggregátokon — `private set` vagy `init` (EF Core: `private set`)
2. Business logic csak Domain-ben
3. Minden mutáció domain eventet ráz
4. PopDomainEvents() + dispatch minden mutating handler végén
5. Minden lista lekérdezés Ardalis.Specification-ön át
6. Result<T> minden handler return type-ja
7. ConfigureAwait(false) minden production async callban
8. AsNoTracking() minden read-only method-ban

## Kritikus szabályok
- `QuoteNumberGenerator`: `pg_advisory_xact_lock(hashtext(tenantId::text))` — race-free
- `CompleteConversion()`: archive guard — ha Quote már archived, dobjon domain error-t
- `ISalesWorkerDbContextFactory`: tenant assert (`DiD`) minden worker iterációban
- `JoineryOrderConversionClient`: header-body single source (TenantId egyszerre írva header + body)
- Optimistic concurrency: `xmin` rowversion minden aggregate-en
- Rate limiter: minden publikus endpoint (§8 security gate)
- JWT lockdown: `[Authorize]` minden endpoint, RBAC policy

## DoD: ≥88 új teszt (30 domain + 22 handler + 14 API + 18 security + 4 concurrency)

## KÖTELEZŐ PIPELINE — MINDEN FELADATRA

⚠️ Minden lépés kötelező. Kihagyni TILOS.

```
INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX
```

### 1. INBOX READ
```bash
grep -rl "status: UNREAD" ./mailbox/inbox/ 2>/dev/null
ls -lt ./mailbox/inbox/ | grep "^-" | head -3
```
Frontmatter: `status: UNREAD` → `status: READ`

### 2. CODE
Implementálj a spec szerint (`docs/tasks/active/SpaceOS_Modules_Sales_Architecture_v4.md`)

### 3. BUILD
`dotnet build` → **0 error, 0 warning** — ha nem: javítsd, ne lépj tovább

### 4. TEST
`dotnet test` → **minden teszt zöld** — ha nem: javítsd, ne lépj tovább

### 5. REVIEW (önellenőrzés)
- Layer dependency rule betartva?
- Nincs public setter az aggregate-eken?
- Business logic csak Domain-ben?
- `ConfigureAwait(false)` minden async callban?
- `AsNoTracking()` minden read-only lekérdezésben?
- `xmin` rowversion beállítva?
- Nincs `TODO`/`FIXME` a kódban?

### 6. SECURITY ⚠️
- JWT `[Authorize]` + RBAC policy minden endpoint-on?
- RLS FORCE minden tenant-scoped táblán?
- Rate limiter konfigurálva?
- `QuoteNumberGenerator` advisory lock implementálva?
- `X-SpaceOS-Internal` header header-body strict equal?
- Worker: tenant `DiD` assert aktív?
- OWASP Top 10: nincs nyilvánvaló sebezhetőség?

### 7. OUTBOX ⚠️ SOHA NEM HAGYHATÓ KI
Fájlnév: `YYYY-MM-DD_NNN_<slug>-done.md` → `./mailbox/outbox/`

```yaml
---
id: MSG-SALES-NNN-DONE
from: sales
to: conductor
type: done
priority: high
status: UNREAD
ref: MSG-SALES-NNN
created: YYYY-MM-DD
---
```

**Ha elakadtál:** `status: BLOCKED` outbox üzenettel jelezz — ne folytasd találgatással.

---

## Közös erőforrások

- **Inbox**: `./mailbox/inbox/` → `/opt/spaceos/docs/mailbox/sales/inbox/`
- **Outbox**: `./mailbox/outbox/` → `/opt/spaceos/docs/mailbox/sales/outbox/`
- **Spec**: `docs/tasks/active/SpaceOS_Modules_Sales_Architecture_v4.md`
- **Codebase_Status.md**: `/opt/spaceos/docs/Codebase_Status.md`
- **WORKFLOW.md**: `/opt/spaceos/docs/WORKFLOW.md`
- **Sablon (solution struktúra)**: `/opt/spaceos/backend/spaceos-modules-cutting/`
- **`/spaceos-terminal` skill**: `/opt/spaceos/.claude/skills/spaceos-terminal/`
