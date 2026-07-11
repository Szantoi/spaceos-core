# CLAUDE.md — spaceos-modules-identity

> Global rules. Every sub-folder's CLAUDE.md inherits these.

---

## SESSION STARTUP/SHUTDOWN RITUAL

**Minden session elején:**
```bash
# 0. Datahaven státusz regisztráció — jelezd hogy dolgozol
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "identity",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'

# 1. Inbox ellenőrzés
ls /opt/spaceos/docs/mailbox/identity/inbox/
grep -l "status: UNREAD" /opt/spaceos/docs/mailbox/identity/inbox/*.md 2>/dev/null
```

**Session végén (DONE/BLOCKED outbox után):**
```bash
# Datahaven státusz regisztráció — jelezd hogy befejeztél
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{\"terminal\":\"identity\",\"status\":\"idle\"}'
```

**Datahaven Dashboard:** https://datahaven.joinerytech.hu (token: `dev-token-spaceos-dashboard-2026`)
- Dashboard (`/`) — Identity státusz (WORKING/IDLE), inbox/outbox metrikák
- Kanban (`/kanban`) — Identity swimlane a Delivery track-en
- Teljes API: `docs/WORKFLOW.md` — "Datahaven Dashboard" szakasz

---

## JELENLEGI ÁLLAPOT

| | |
|---|---|
| **Terminál** | identity · Port: **5008** (systemd, loopback-only) · Mailbox: `/opt/spaceos/docs/mailbox/identity/` |
| **Repo** | `/opt/spaceos/backend/spaceos-modules-identity` |
| **Aktuális commit** | — (új service, még nincs commit) |
| **Tesztek** | — (implementáció folyamatban) |
| **VPS** | nem deployolva — Production GA blocker: P0-1 (JWT RS256) |

### ⚠️ PRODUCTION GA BLOCKER

**P0-1: JWT HS256 → RS256 migráció (Kernel scope)** — lezárásáig az Identity modul
implementálható és tesztelhető, de **production deploy TILOS**.

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/identity.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## PROJEKT VÍZIÓ — MIÉRT ÉPÜL EZ A RENDSZER

> **A SpaceOS a magyar faipar digitális gerince** — iparspecifikus SaaS platform,
> amely ajtógyártókat, szekrénygyártókat, lapszabászokat, kereskedőket és beszerelőket
> egyetlen összekapcsolt ökoszisztémába szervezi.

**A probléma:** A faiparos KKV-k 90%+ ma Viber + Excel + telefon alapon koordinál.
Nincs rájuk szabott, megfizethető digitális megoldás — ezt az űrt tölti be a SpaceOS.

**Rendszer (4 réteg):**
```
L4  JoineryTech Portal       React 18 — brand-specifikus UI
L3  Orchestrator             Node.js 22 — LLM Tool Calling, AI gateway
L2  Modules (Drivers)        .NET 8 — iparági üzleti logika
    └── Identity ← EZ A REPO  User management, KC Admin API integration
L1  Kernel                   .NET 8 + PostgreSQL — auth, FSM, audit
```

**Első éles ügyfél:** Doorstar Kft. (ajtógyártó) — Soft Launch: **2026 Q2**

---

## AZ IDENTITY MODUL SZEREPE

**Egyetlen authoritative forrás** a user és tenant membership domain-re.

```
Keycloak (KC 24.0)
└── AuthN only: login · JWT · MFA · session · keycloak_user_id generálás

spaceos-modules-identity (5008)
├── Authoritative: SpaceOSUser · (TenantMembership v2 — jövőbeli)
├── Write-through → Keycloak (via Outbox + KcSyncWorkerService)
└── KC soha nem ír SpaceOS-ba — egyirányú szinkron
```

**Write-through sorrend — INVARIANT:**
```
1. DB tranzakció: INSERT spaceos_users + INSERT kc_sync_outbox  (atomiáris)
2. KcSyncWorkerService: feldolgozza az outbox-ot (BackgroundService)
3. KC Admin API hívás (Polly 3×, exponential backoff)
4. Success → kc_sync_outbox DELETE + kc_sync_status = Synced
5. Final failure → kc_sync_status = Failed + UserKcSyncFailedEvent
```

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/identity.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## PROJECT SNAPSHOT

**Solution:** `SpaceOS.Modules.Identity` — Clean Architecture, DDD, CQRS, Outbox pattern, .NET 8 LTS
**Build:** célállapot: 0 errors, 0 warnings
**Tests:** célállapot: ≥ 50 teszt

```
Identity.Domain/
  Aggregates/SpaceOSUser.cs
  ValueObjects/  SpaceOSUserId · KeycloakUserId · Email · DisplayName · UserStatus · KcSyncStatus
  DomainEvents/  UserCreatedEvent · UserProfileUpdatedEvent · UserDisabledEvent · UserEnabledEvent
                 PasswordResetRequestedEvent · UserKcSyncFailedEvent
  Interfaces/    ISpaceOSUserRepository · IIdentityProviderClient

Identity.Application/
  Users/
    Queries/   ListTenantUsersQuery · GetUserByIdQuery
    Commands/  CreateUserCommand · UpdateUserProfileCommand · DisableUserCommand
               EnableUserCommand · ResetPasswordCommand · SyncTenantUsersFromKeycloakCommand
  Common/    ICurrentUserContext · DTOs/

Identity.Infrastructure/
  Persistence/   IdentityDbContext · Configurations/ · Repositories/ · Migrations/
  Keycloak/      KeycloakAdminClient · KeycloakTokenProvider · Models/
  Workers/       KcSyncWorkerService  ← BackgroundService, Polly 3×
  Cache/         UserCacheService     ← Redis cache-aside, 30s TTL
  CurrentUser/   CurrentUserContext

Identity.Api/
  Controllers/   UsersController · AdminController
  Program.cs

Identity.Tests/
  Domain/ · Application/ · Infrastructure/
```

**Layer dependency rule (hard constraint):**
```
Domain ← Application ← Infrastructure
Domain has ZERO external NuGet dependencies.
```

---

## API SURFACE

| Method | Path | Policy | Leírás |
|---|---|---|---|
| `GET` | `/identity/users` | `TenantMember` | Tenant userek listája |
| `GET` | `/identity/users/{id}` | `TenantMember` | Egy user — explicit BOLA guard |
| `POST` | `/identity/users` | `TenantAdmin` | Create + Outbox insert |
| `PUT` | `/identity/users/{id}` | `TenantAdmin` | Profil update |
| `POST` | `/identity/users/{id}/disable` | `TenantAdmin` | Disable |
| `POST` | `/identity/users/{id}/enable` | `TenantAdmin` | Enable |
| `POST` | `/identity/users/{id}/reset-password` | `TenantAdmin` | Rate limited: 5/user/hour |
| `POST` | `/identity/admin/tenants/{id}/sync-from-keycloak` | `SuperAdmin` | Bootstrap sync |

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/identity.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## APPROVED PACKAGES

```xml
<!-- Identity.Infrastructure -->
-- Keycloak.AuthServices.Sdk: NEM HASZNÁLT (net10.0 igényel, net8.0 inkompatibilis)
-- Helyette: plain HttpClient (KeycloakAdminClient.cs) — ekvivalens funkcionalitás
StackExchange.Redis 2.8.16
Polly 8.4.1
Polly.Extensions.Http 3.0.0

<!-- Identity.Api -->
Keycloak.AuthServices.Authentication 2.8.0

<!-- Shared — Kernel-lel azonos verziók -->
MediatR 12.4.0
FluentValidation.AspNetCore 11.3.0
Ardalis.Result 9.0.0
Ardalis.Specification.EntityFrameworkCore 9.1.0
Microsoft.EntityFrameworkCore 8.0.11
Npgsql.EntityFrameworkCore.PostgreSQL 8.0.11
Serilog.AspNetCore 8.0.1

<!-- Tests -->
xunit 2.9.2
Moq 4.20.72
Microsoft.AspNetCore.Mvc.Testing 8.0.11
```

Listán kívüli csomag → megbeszélés előtt ne add hozzá.

---

## NAMING CONVENTIONS

| Scope | Convention |
|---|---|
| Classes, methods, properties | `PascalCase` |
| Private fields | `_camelCase` |
| Local variables | `camelCase` |
| CancellationToken param | mindig `ct` |
| File name | 1:1 class névvel |
| Feature folders | `Users/`, `Commands/`, `Queries/`, `Events/` |

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/identity.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## UNIVERSAL CODE RULES

```csharp
// 1. ConfigureAwait(false) minden production async callban
await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);

// 2. CancellationToken neve mindig ct
public async Task<Result<T>> Handle(TRequest request, CancellationToken ct)

// 3. Result<T> minden handleren — soha ne dobjon exception-t kliensnek
return Result.Forbidden();   // nem throw UnauthorizedException

// 4. AsNoTracking() minden read-only repository metóduson
return await _db.SpaceOSUsers.AsNoTracking().Where(...).ToListAsync(ct);

// 5. SET LOCAL minden mutating repo metódus elején (RLS, DB-05)
await _db.Database.ExecuteSqlRawAsync("SET LOCAL app.current_tenant_id = {0}", tenantId);
```

---

## BIZTONSÁGI SZABÁLYOK (kötelező)

| # | Szabály |
|---|---|
| SEC-01 | KC hívás előtt: assert `kc_user.tid == currentUser.TenantId` |
| SEC-02 | Minden GET handler: explicit `user.TenantId != _currentUser.TenantId → Result.Forbidden()` |
| SEC-03 | Redis token cache: AES-256-GCM titkosítva |
| SEC-04 | `reset-password`: Redis sliding window, 5/user/hour, 429 RFC 7807 |
| SEC-05 | KC hibák: `IdentityProviderException` wrap — nem proxyzva a kliensnek |
| SEC-06 | `sync-from-keycloak`: tid mismatch → skip + warn (nem import) |
| SEC-07 | `audit_log` INSERT minden write handler végén |
| SEC-08 | Email nem plaintext logban — Serilog `email_masked` |
| SEC-09 | `tid` kizárólag JWT-ből — header/body/querystring elfogadás TILTVA |

Részletes minták: `.claude/skills/identity-security/SKILL.md`

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/identity.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## OUTPUT FORMAT

- **Fájl path** minden kódblokk első sorában:
  ```csharp
  // Identity.Domain/Aggregates/SpaceOSUser.cs
  ```
- **Diff preferred** — teljes fájl csak ha teljesen újraírva
- **Implementáció után:** summary táblázat

  | Fájl | Változás | Ok |
  |---|---|---|

- **Nincs TODO/FIXME** commitolt kódban
- **Minden új osztályhoz** teszt (`Identity.Tests/` megfelelő mappájában)

---

## ISMERT GOTCHÁK (Track A–E tapasztalat)

| Probléma | Megoldás |
|---|---|
| `Keycloak.AuthServices.*` v2.8.0 — net10.0 igényel | plain `HttpClient` + `JwtBearer 8.0.11` — ekvivalens |
| `Ardalis.Result<T>.ToActionResult()` returns `ActionResult<T>` nem `IActionResult` | `IdentityControllerBase.Respond<T>()` via `IConvertToActionResult.Convert()` |
| `WebApplicationFactory` + `AllowedHosts: "127.0.0.1"` → TestServer 400 | `builder.UseSetting("AllowedHosts", "*")` a factory-ban |
| `ConfigureKestrel(ListenLocalhost(5008))` kódban → TestServer 400 | port binding csak `appsettings.Production.json` Kestrel:Endpoints-ban |
| Redis `ConnectionMultiplexer` startup crash ha Redis elérhetetlen | `AbortOnConnectFail=false` + try-catch a token providerben |
| `dotnet-ef` v10.0.7 net8.0 inkompatibilis a szerveren | INFRA telepíti a `dotnet-ef 8.x` tool-t deploy előtt |

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/identity.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## BEHAVIORAL RULES

| Szituáció | Teendő |
|---|---|
| Bizonytalan a meglévő kódban | Olvasd el — soha ne találgass |
| Golden Rule sérülne | Jelezd egyszer, majd hajtsd végre |
| Elavult csomag | Jelezd + javasolj alternatívát, várj döntésre |
| Breaking change (interfész módosítás, schema change) | **Stop. Confirm before proceeding.** |
| Production deploy kérés | **BLOKKOLVA — P0-1 lezárásáig tilos** |

---

## KÖTELEZŐ PIPELINE — MINDEN FELADATRA

⚠️ Minden lépés kötelező. Kihagyni TILOS.

```
INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX
```

### 1. INBOX READ
```bash
grep -rl "status: UNREAD" ./mailbox/inbox/ 2>/dev/null
# vagy:
ls -lt ./mailbox/inbox/ | grep "^-" | head -3
```
`status: UNREAD` → `status: READ`

### 2. CODE — spec: `docs/tasks/new/SpaceOS_Modules_Identity_Architecture_v4.md`

### 3. BUILD
```bash
dotnet build
```
→ **0 error, 0 warning** — ha nem: javítsd, ne lépj tovább

### 4. TEST
```bash
dotnet test
```
→ **minden teszt zöld** — ha nem: javítsd, ne lépj tovább

### 5. REVIEW
- Layer dependency: `Domain ← Application ← Infrastructure` betartva?
- `ConfigureAwait(false)` minden async callban?
- `AsNoTracking()` minden read-only metóduson?
- `SET LOCAL` minden mutating metódus elején?
- Nincs `TODO`/`FIXME`?

### 6. SECURITY — `.claude/skills/identity-security/SKILL.md` checklist

### 7. OUTBOX ⚠️ SOHA NEM HAGYHATÓ KI
Fájlnév: `YYYY-MM-DD_NNN_[slug]-done.md` → `./mailbox/outbox/`

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/identity.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## KÖZÖS ERŐFORRÁSOK

- **Inbox**: `./mailbox/inbox/` (lokális) + `/opt/spaceos/docs/mailbox/identity/inbox/`
- **Outbox**: `./mailbox/outbox/` (lokális) + `/opt/spaceos/docs/mailbox/identity/outbox/`
- **Spec**: `docs/tasks/new/SpaceOS_Modules_Identity_Architecture_v4.md`
- **Codebase_Status.md**: `/opt/spaceos/docs/Codebase_Status.md`
- **WORKFLOW.md**: `/opt/spaceos/docs/WORKFLOW.md`
- **Keycloak Outbox skill**: `.claude/skills/keycloak-outbox/SKILL.md`
- **Security skill**: `.claude/skills/identity-security/SKILL.md`
- **Terminal protokoll**: `.claude/skills/spaceos-terminal/SKILL.md`

---

## BACKEND IMPLEMENTÁCIÓS CHECKLIST

Minden feature/bugfix végén, DONE outbox előtt:

- [ ] Entity creation factory method-dal (nem publikus constructor)
- [ ] Setter-ek private-ok
- [ ] Domain validation implementálva (nem controller-ben)
- [ ] Controller/endpoint csak DTO-t ad vissza (entity soha)
- [ ] Unit test üzleti logikára
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` minden zöld

### QA Handoff kritérium

A TESTER terminált ROOT hívja be ha a feladat:
- Üzleti validációs logikát tartalmaz (pl. rendelés állapotgép, ár kalkuláció)
- Pénzügyi számítást végez
- Workflow / FSM state machine-t érint
- A task explicit jelzi: "QA needed: Yes"

Egyszerű CRUD endpoint-ok NEM igényelnek QA-t, kivéve ha explicit kérve van.
