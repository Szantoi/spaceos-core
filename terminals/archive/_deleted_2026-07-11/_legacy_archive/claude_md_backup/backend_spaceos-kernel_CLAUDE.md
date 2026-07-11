# CLAUDE.md — SpaceOS.Kernel (Root)

> Global rules. Every layer's CLAUDE.md inherits these.
> Layer-specific rules are in each project's own CLAUDE.md.

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/kernel.md`

**DONE előtt:** Frissítsd a memory fájlt!

---

## SESSION STARTUP/SHUTDOWN RITUAL

**Minden session elején:**
```bash
# 0. Datahaven státusz regisztráció — jelezd hogy dolgozol
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "kernel",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'

# 1. Inbox ellenőrzés
ls /opt/spaceos/docs/mailbox/kernel/inbox/
grep -l "status: UNREAD" /opt/spaceos/docs/mailbox/kernel/inbox/*.md
```

**Session végén (DONE/BLOCKED outbox után):**
```bash
# Datahaven státusz regisztráció — jelezd hogy befejeztél
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"kernel","status":"idle"}'
```

**Datahaven Dashboard:** https://datahaven.joinerytech.hu (token: `dev-token-spaceos-dashboard-2026`)
- Dashboard (`/`) — Kernel státusz (WORKING/IDLE), inbox/outbox metrikák
- Kanban (`/kanban`) — Kernel swimlane a Delivery track-en
- Teljes API: `docs/WORKFLOW.md` — "Datahaven Dashboard" szakasz

---

## JELENLEGI ÁLLAPOT (2026-04-17)

| | |
|---|---|
| **Terminál** | kernel · Port: **5000** (systemd, loopback-only) · Mailbox: `/opt/spaceos/docs/mailbox/kernel/` |
| **Repo** | `/opt/spaceos/spaceos-kernel` |
| **Aktuális commit** | `130959a` (KERNEL-085: DELETE /internal/flow-epics/by-tenant) |
| **Tesztek** | **1121/1121 pass** (910 unit + 107 + 104 IT) |
| **VPS** | LIVE ✅ · /healthz 200 ✅ |

### Fontos: RLS nem GUC-alapú a Kernelben
A Kernel `IgnoreQueryFilters()` + explicit `WHERE tenantId = ...` WHERE használ — **NEM** függ a
`app.current_tenant_id` GUC értékétől. Ez szándékos architektúrális döntés.

### Internal DELETE endpoint (4-gate security)
```
DELETE /internal/flow-epics/by-tenant?tenantId={guid}&confirm=true
Headers: X-SpaceOS-Internal: true
```
Ugyanez minden modul: joinery, cutting, inventory, procurement.

### FlowEpic létrehozás — KÖTELEZŐ sorrend
```
POST /api/tenants/{tenantId}/facilities   → facilityId
POST /api/facilities/{facilityId}/flow-epics  + { title }  → epicId
```
`POST /api/flow-epics` közvetlenül nem létezik.

---

## PROJEKT VÍZIÓ — MIÉRT ÉPÜL EZ A RENDSZER

> **A SpaceOS a magyar faipar digitális gerince** — iparspecifikus SaaS platform,
> amely ajtógyártókat, szekrénygyártókat, lapszabászokat, kereskedőket és beszerelőket
> egyetlen összekapcsolt ökoszisztémába szervezi.

**A probléma:** A faiparos KKV-k 90%+ ma Viber + Excel + telefon alapon koordinál.
Nincs rájuk szabott, megfizethető digitális megoldás — ezt az űrt tölti be a SpaceOS.

**Rendszer (4 réteg):**
```
L4  Design Portal / JoineryTech   React 18 — brand-specifikus UI-k
L3  Orchestrator (BFF)            Node.js 22 — LLM Tool Calling, API gateway
L2  Modules (Drivers)             .NET 8 — iparági üzleti logika (Joinery, MEP, Pricing)
L1  Kernel  ← EZ A REPO          .NET 8 + PostgreSQL — auth, audit, FSM, escrow
```

**Első éles ügyfél:** Doorstar Kft. (ajtógyártó) — Soft Launch: **2026 Q2**

**Célpiac (HU):** 1300–2500 cég (ajtó, szekrény, lapszabász, ablak, kereskedő)
**Roadmap:** 2026 Q3 — szabászat + 2. ügyfél · 2027 — 5+ ügyfél, DACH belépés

**Üzleti modell:** SaaS előfizetés (moduláris) + B2B tranzakciós díj + hálózati hatás

**5 Golden Rule (minden architektúra-döntésnél kötelező):**

| # | Szabály |
|---|---|
| 1 | **Data → Rules → Geometry** — frontend rajzol, C# Driver számol, LLM csak paramétereket ad |
| 2 | **Modular Monolith** — Kernel `IParametricProduct` interfészen dolgozik, nem tudja mi az asztalos |
| 3 | **Immutability & Trust** — nincs UPDATE CAD adatokon, minden SHA-256 hashed audit eventtel |
| 4 | **Need-to-Know RBAC** — megrendelő nem látja a gyártó belső anyaglistáját |
| 5 | **Walking Skeleton First** — E2E pipeline előbb, matematika utóbb mélyül |

> Teljes vízió: `/opt/spaceos/docs/SpaceOS_Vision_Results_20260413.md`
> Technikai master: `/opt/spaceos/docs/vision/SpaceOS_Vision_Master.md`

---

## PROJECT SNAPSHOT

**Solution:** `SpaceOS.Kernel` — Clean Architecture, DDD, CQRS, .NET 8 LTS
**Build:** 0 errors, 0 warnings | **Tests:** 1077 passing | **Auth:** Keycloak RS256 JWKS (`spaceos_tenants` claim)

```
SpaceOS.Kernel.Domain              ← aggregates, VOs, domain events, repository interfaces
SpaceOS.Kernel.Application         ← CQRS handlers, validators, DTOs, event handlers
SpaceOS.Infrastructure             ← EF Core 8 + PostgreSQL (Npgsql 8.0.11)
SpaceOS.Modules.FlowManagement     ← modular domain: FlowTask, FlowMilestone, FlowProject, FlowProgram
SpaceOS.Kernel.Tests               ← xUnit v3, Moq — unit tests
SpaceOS.Kernel.IntegrationTests    ← xUnit v3 — repository + pipeline integration tests (SQLite in-memory)
SpaceOS.Kernel.Api.Tests           ← xUnit v3, WebApplicationFactory — API integration tests
```

**Layer dependency rule (hard constraint):**
```
Domain ← Application ← Infrastructure
```
Domain has zero external NuGet dependencies. Any violation → flag immediately, do not proceed.

---

## APPROVED PACKAGES

MediatR 12.4.1 · FluentValidation 12.1.1 · Ardalis.Result 10.1.0
Ardalis.Specification 8.0.0 · EF Core 8 · xUnit v3 · Moq

Adding anything outside this list requires explicit discussion first.

---

## NAMING CONVENTIONS

| Scope | Convention |
|---|---|
| Classes, methods, properties | `PascalCase` |
| Private fields | `_camelCase` |
| Local variables | `camelCase` |
| CancellationToken param | always `ct` |
| File name | 1:1 with class name |
| Feature folders | `Tenants/`, `Facilities/`, `Commands/`, `Queries/`, `Events/` |

---

## UNIVERSAL CODE RULES

```csharp
// 1. ConfigureAwait(false) on every production async call
await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);

// 2. CancellationToken always named ct
public async Task<Result<T>> Handle(TRequest request, CancellationToken ct)

// 3. XML docs on every public type and method
/// <summary>...</summary>
```

---

## OUTPUT FORMAT

- **Lead with action** — no preamble, no summary at the end
- **Table > prose** always
- **File path** as first line in every code block:
  ```csharp
  // SpaceOS.Kernel.Domain/Tenants/Tenant.cs
  ```
- **Diff preferred** — full file only if entirely rewritten
- **After implementation:** summary table only

  | File | Change | Reason |
  |---|---|---|

- **No TODO/FIXME** in committed code
- **Next steps:** only if explicitly asked

---

## BEHAVIORAL RULES

| Situation | Action |
|---|---|
| Uncertain about existing code | Read the file first — never guess |
| Request violates a pattern | Warn once, then execute |
| Deprecated package | Flag + propose alternative, await decision |
| Architectural decision | Explain options first, implement after approval |
| Breaking change (interface mod, file delete, schema change) | **Stop. Confirm before proceeding.** |
| Tooling already decided | No opinion — just implement |

---

## GOLDEN RULES

| # | Rule | Description |
|---|------|-------------|
| #9 | **Data Sovereignty** | A Node saját adatai fizikailag el vannak választva. A Kernel csak létezésről és FSM státuszról tud. Tartalom soha nem kerül a Kernelbe — csak HMAC hash. |
| #10 | **Offline First** | Minden Node képes önállóan üzemelni kapcsolat nélkül. A sync eventual consistency. Az OfflineQueueService garantálja, hogy semmi el nem veszik. |
| #11 | **Security by Default** | Minden Node-to-Node kommunikáció TLS 1.3 + signed Node JWT. Minden érzékeny DB oszlop AES-256-GCM titkosítva. Minden URL SSRF-validált. Minden RLS scope DB szinten enforceolt. |
| #12 | **Transactional Integrity** | FSM átmenet + SyncSignal + AuditEvent egyetlen UnitOfWork tranzakcióban. Hash chain FOR UPDATE-tel serialized. PopDomainEvents() csak sikeres commit után. |

---

## CURRENT SESSION GOAL

> ✏️ E2E stabilizálás — 2026-04-13

```
Context: SpaceOS.Kernel — E2E regresszió javítás + VPS deploy pending

Legutóbbi commit: 316f603 (develop)
  - ClaimsTenantResolver: JsonDocument parsing, spaceos_tenants → tid → groups fallback
  - TenantSessionInterceptor: GUID-norm + string.Equals fallback az Active-Tenant headerre
  - 1084 teszt zöld

Aktuális állapot:
  - VPS-en c62f1d7 fut (rollback, 8dd0bd7 regresszió miatt)
  - 316f603 deploy pending → INFRA terminál feladata (MSG-INFRA-064)
  - E2E utolsó futás: 119/151 (8dd0bd7-n) → deploy után várható: 147+/151

Utolsó migration: 0028_StageRegistry
Következő feladat: E2E deploy után újrafuttatás verifikálja a javításokat

Key tenant resolution rules (auth layer):
  - ClaimsTenantResolver: spaceos_tenants JSON array → tid → groups → null (soha nem throw)
  - TenantSessionInterceptor: tid → spaceos_tenants → tenant_id → sentinel UUID (MSG-067 fix)
  - AppDbContext global filter: CurrentTenantGuid = ClaimsTenantResolver.TryResolve()?.Value
  - app.current_tenant_id (PG session): TenantSessionInterceptor állítja be
  - ⚠️ Mind a kettőnek UGYANOLYAN UUID-t kell visszaadnia — különben RLS/filter mismatch
```

---

## KÖTELEZŐ PIPELINE — MINDEN FELADATRA

⚠️ Minden lépés kötelező. Kihagyni TILOS. Lásd teljes leírás: `/opt/spaceos/docs/WORKFLOW.md`

```
INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX
```

### 1. INBOX READ
- `ls ./mailbox/inbox/` → legfrissebb UNREAD üzenet elolvasása
- Frontmatter: `status: UNREAD` → `status: READ`

### 2. CODE
- Implementálj a feladat szerint

### 3. BUILD
- `dotnet build` → **0 error, 0 warning** — ha nem: javítsd, ne lépj tovább

### 4. TEST
- `dotnet test` → **minden teszt zöld** — ha nem: javítsd, ne lépj tovább
- Új kódhoz új tesztet írj

### 5. REVIEW (önellenőrzés)
- Layer dependency rule: `Domain ← Application ← Infrastructure` betartva?
- Naming conventions OK? (`PascalCase`, `_camelCase`, `ct`)
- `ConfigureAwait(false)` minden async callban?
- Nincs `TODO`/`FIXME` a kódban?
- Nincs üzleti logika az Infrastructure rétegben?

### 6. SECURITY ⚠️
- **Input validation**: minden külső input validálva van?
- **Authorization**: minden endpoint `[Authorize]`-zal védett?
- **RLS**: érintett táblák RLS policy-val rendelkeznek?
- **SQL injection**: csak paraméteres query, soha string concat?
- **Sensitive data**: jelszó/token nem kerül logba?
- **OWASP Top 10**: nincs nyilvánvaló sebezhetőség?

### 7. OUTBOX ⚠️ SOHA NEM HAGYHATÓ KI
Minden befejezett feladat után kötelező outbox üzenetet írni.
Fájlnév: `YYYY-MM-DD_NNN_[slug]-done.md` → `./mailbox/outbox/`

```markdown
---
id: MSG-KXXX-DONE
from: kernel
to: conductor
type: done
status: UNREAD
---

## Összefoglaló
[Mit implementáltál, mely fájlok változtak]

## Tesztek
[Hány teszt futott, mind zöld? Új tesztek száma?]

## Security review
[Mely pontokat ellenőrizted (RLS, auth, input validation, stb.)]

## Kockázatok / kérdések
[Ha van → status: BLOCKED és leírás]
```

**Ha elakadtál:** `status: BLOCKED` outbox üzenettel jelezz — ne folytasd találgatással.

---

## Közös erőforrások

- **Inbox**: `./mailbox/inbox/`
- **Outbox**: `./mailbox/outbox/`
- **Codebase_Status.md**: `./docs/Codebase_Status.md` — frissítsd minden sprint után
- **WORKFLOW.md**: `/opt/spaceos/docs/WORKFLOW.md` — teljes munka módszertan
- **Projekt vízió (üzleti)**: `/opt/spaceos/docs/SpaceOS_Vision_Results_20260413.md` — miért épül a rendszer, Doorstar first customer, célpiac
- **Technikai master overview**: `/opt/spaceos/docs/vision/SpaceOS_Vision_Master.md` — 4 réteg, 5 Golden Rule, domain modell, döntési fa
- **`/spaceos-terminal` skill**: inbox olvasás + build/test gate + DONE/BLOCKED outbox protokoll — `/opt/spaceos/.claude/skills/spaceos-terminal/`
