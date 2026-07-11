# SpaceOS.Modules.Joinery — CLAUDE.md

## SESSION STARTUP/SHUTDOWN RITUAL

**Minden session elején:**
```bash
# 0. Datahaven státusz regisztráció — jelezd hogy dolgozol
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "joinery",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'

# 1. Inbox ellenőrzés
ls /opt/spaceos/docs/mailbox/joinery/inbox/
grep -l "status: UNREAD" /opt/spaceos/docs/mailbox/joinery/inbox/*.md 2>/dev/null
```

**Session végén (DONE/BLOCKED outbox után):**
```bash
# Datahaven státusz regisztráció — jelezd hogy befejeztél
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"joinery","status":"idle"}'
```

**Datahaven Dashboard:** https://datahaven.joinerytech.hu (token: `dev-token-spaceos-dashboard-2026`)
- Dashboard (`/`) — Joinery státusz (WORKING/IDLE), inbox/outbox metrikák
- Kanban (`/kanban`) — Joinery swimlane a Delivery track-en
- Teljes API: `docs/WORKFLOW.md` — "Datahaven Dashboard" szakasz

---

## JELENLEGI ÁLLAPOT (2026-04-17)

| | |
|---|---|
| **Terminál** | joinery · Port: **5002** · Mailbox: `/opt/spaceos/docs/mailbox/joinery/` |
| **Aktuális commit** | `61defca` (JOINERY-014: OpenConnectionAsync affinity fix) |
| **Tesztek** | **219/219 pass** |
| **VPS** | LIVE ✅ |

### Kritikus technikai konstans — NE FELEDD
```
TenantGucKey = "app.tenant_id"   ← JOINERY-SPECIFIKUS, NEM "app.current_tenant_id"
```
A többi modul `app.current_tenant_id`-t használ. A Joinery `app.tenant_id`-t. Ez RLS bug forrása volt.

### InternalEndpoints.cs — OpenConnectionAsync minta (KÖTELEZŐ)
Az `InternalEndpoints.cs` DELETE by-tenant handlerében a GUC set_config és a DeleteAllByTenantAsync
ugyanazon fizikai connection-ön KELL hogy fusson:
```csharp
if (db.Database.IsRelational())
    await db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
try {
    if (db.Database.IsRelational())
        await db.Database.ExecuteSqlRawAsync($"SELECT set_config('{TenantGucKey}', {{0}}, false)", tenantGuid.ToString()).ConfigureAwait(false);
    counts = await repo.DeleteAllByTenantAsync(tenantGuid, ct).ConfigureAwait(false);
} finally {
    if (db.Database.IsRelational())
        await db.Database.CloseConnectionAsync().ConfigureAwait(false);
}
```

### OutboxWorker
- Tábla: `spaceos_joinery."JoineryOutboxEntries"` (schema prefix KÖTELEZŐ)
- `relforcerowsecurity = f` — az INFRA-136 eltávolította a FORCE RLS-t (2026-04-17)

---

## Stack
- .NET 8, Clean Architecture + DDD + CQRS
- PostgreSQL 16 schema: spaceos_joinery
- EF Core 8 + Npgsql 8.0.11

## Approved packages
MediatR 12.4.1 · FluentValidation 12.1.1 · Ardalis.Result 10.1.0 · Ardalis.Specification 8.0.0
EF Core 8.0.11 · Npgsql 8.0.11 · xUnit v3 · Moq 4.20.72 · FluentAssertions 6.12.2

Adding anything outside this list requires explicit discussion first.

## Solution structure
```
SpaceOS.Modules.Joinery.Domain         ← aggregates, VOs, domain events, service interfaces
SpaceOS.Modules.Joinery.Application    ← CQRS handlers, validators, DTOs
SpaceOS.Modules.Joinery.Infrastructure ← EF Core 8 + PostgreSQL, service implementations
SpaceOS.Modules.Joinery.Api            ← Minimal API endpoints
SpaceOS.Modules.Joinery.Tests          ← xUnit v3, Moq — unit + integration tests
```

## Layer dependency rule (hard constraint)
```
Domain ← Application ← Infrastructure ← Api
                                       ← Tests
```
Domain has zero external NuGet dependencies (except Ardalis.Result). Any violation → flag immediately.

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
_db.DoorOrders.AsNoTracking().Where(...)

// 4. Result<T> minden handler return type
public async Task<Result<CuttingListResponse>> Handle(...)
```

## Golden Rules
1. Nincs public setter az aggregátokon
2. Business logic csak Domainben
3. Minden mutáció domain eventet ráz
4. PopDomainEvents() + dispatch minden mutating handler végén
5. Minden lista lekérdezés Ardalis.Specification-ön át
6. Result<T> minden handler return type-ja
7. ConfigureAwait(false) minden production async callban
8. AsNoTracking() minden read-only method-ban

## Kritikus szabályok
- CuttingList SOHA nem cache-elhető — mindig on-demand (SEC-05)
- DoorOrder.AddItem() csak Draft státuszban (BE-04)
- IDoorCalculationService: pure function — NINCS DateTime.Now, Random, I/O
- PartDimensionRule: WidthBase + WidthMultiplierFactor — NINCS szabad formula eval (DB-02)
- FlowEpicId: minden DoorOrder-nek van — Handshake teremt Epic-et (ADR-008)
- MaxItems = 500 per order (SEC-07)
- Minden endpoint: [Authorize(Policy = "ManufacturerOnly")] (SEC-04)

## DB schema: spaceos_joinery
- DoorOrders: RLS FORCE (TenantId alapján)
- DoorItems: RLS FORCE (subquery: OrderId IN DoorOrders WHERE TenantId = ...)
- DoorTypeRules, PartDimensionRules, CuttingConstants, ProcessTaskTemplates, GlobalConstants: tenant-független config
- GlobalConstants: REVOKE INSERT/UPDATE/DELETE FROM spaceos_app — csak SELECT

## API surface
```
POST   /api/orders                    CreateDoorOrder
POST   /api/orders/{id}/items         AddDoorItem
POST   /api/orders/{id}/calculate     CalculateDoorOrder
GET    /api/orders/{id}/cutting-list  GetCuttingList (Cache-Control: no-store)
GET    /api/orders/{id}/process-plan  GetProcessPlan
GET    /api/orders/{id}/hardware-list GetHardwareList
GET    /api/orders/{id}/material-req  GetMaterialRequirements
POST   /api/orders/{id}/submit        SubmitDoorOrder
GET    /api/orders                    ListDoorOrders (paged)
GET    /api/orders/{id}               GetDoorOrder
```

## DoD: ≥65 új teszt (domain 25 · calculation 20 · API 15 · security 5)

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
- Layer dependency rule: `Domain ← Application ← Infrastructure ← Api` betartva?
- Nincs public setter az aggregate-eken?
- Business logic csak Domain-ben?
- `ConfigureAwait(false)` minden async callban?
- `AsNoTracking()` minden read-only lekérdezésben?
- Nincs `TODO`/`FIXME` a kódban?

### 6. SECURITY ⚠️
- **Authorization**: minden endpoint `[Authorize(Policy = "ManufacturerOnly")]`?
- **RLS**: `DoorOrders` és `DoorItems` táblák RLS FORCE-al védve?
- **Input validation**: `MaxItems = 500` limit enforceolva?
- **Calculation safety**: `IDoorCalculationService` pure function marad (nincs DateTime.Now, Random)?
- **CuttingList**: `Cache-Control: no-store` header rajta van?
- **OWASP Top 10**: nincs nyilvánvaló sebezhetőség?

### 7. OUTBOX ⚠️ SOHA NEM HAGYHATÓ KI
Minden befejezett feladat után kötelező outbox üzenetet írni.
Fájlnév: `YYYY-MM-DD_NNN_[slug]-done.md` → `./mailbox/outbox/`

```markdown
## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/joinery.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---
id: MSG-JXXX-DONE
from: joinery
to: conductor
type: done
status: UNREAD
---

## Összefoglaló
[Mit implementáltál, mely fájlok változtak]

## Tesztek
[Hány teszt futott, mind zöld? Új tesztek száma?]

## Security review
[Mely pontokat ellenőrizted (RLS, auth policy, input limits, stb.)]

## Kockázatok / kérdések
[Ha van → status: BLOCKED és leírás]
```

**Ha elakadtál:** `status: BLOCKED` outbox üzenettel jelezz — ne folytasd találgatással.

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/joinery.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## Közös erőforrások

- **Inbox**: `./mailbox/inbox/`
- **Outbox**: `./mailbox/outbox/`
- **Codebase_Status.md**: `./docs/Codebase_Status.md` — frissítsd minden sprint után
- **WORKFLOW.md**: `/opt/spaceos/docs/WORKFLOW.md` — teljes munka módszertan
- **Projekt vízió (üzleti)**: `/opt/spaceos/docs/SpaceOS_Vision_Results_20260413.md` — miért épül a rendszer, Doorstar first customer, célpiac
- **Technikai master overview**: `/opt/spaceos/docs/vision/SpaceOS_Vision_Master.md` — 4 réteg, 5 Golden Rule, domain modell, döntési fa
- **`/spaceos-terminal` skill**: inbox olvasás + build/test gate + DONE/BLOCKED outbox protokoll — `/opt/spaceos/.claude/skills/spaceos-terminal/`

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
