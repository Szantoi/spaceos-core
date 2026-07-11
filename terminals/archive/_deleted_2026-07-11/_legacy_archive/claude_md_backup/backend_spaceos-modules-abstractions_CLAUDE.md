# SpaceOS.Modules.Abstractions — CLAUDE.md

## SESSION STARTUP/SHUTDOWN RITUAL

**Minden session elején:**
```bash
# 0. Datahaven státusz regisztráció — jelezd hogy dolgozol
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "abstractions",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'

# 1. Inbox ellenőrzés
ls /opt/spaceos/docs/mailbox/abstractions/inbox/
grep -l "status: UNREAD" /opt/spaceos/docs/mailbox/abstractions/inbox/*.md 2>/dev/null
```

**Session végén (DONE/BLOCKED outbox után):**
```bash
# Datahaven státusz regisztráció — jelezd hogy befejeztél
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"abstractions","status":"idle"}'
```

**Datahaven Dashboard:** https://datahaven.joinerytech.hu (token: `dev-token-spaceos-dashboard-2026`)
- Dashboard (`/`) — Abstractions státusz (WORKING/IDLE), inbox/outbox metrikák
- Kanban (`/kanban`) — Abstractions swimlane a Delivery track-en
- Teljes API: `docs/WORKFLOW.md` — "Datahaven Dashboard" szakasz

---

## Stack
- .NET 8, Clean Architecture + DDD + CQRS
- PostgreSQL 16 schema: spaceos_modules (in `spaceos` database)
- EF Core 8 + Npgsql 8.0.11
- Port: http://127.0.0.1:5003

## Approved packages
MediatR 12.4.1 · FluentValidation 12.1.1 · Ardalis.Result 10.1.0 · Ardalis.Specification 8.0.0
EF Core 8.0.11 · Npgsql 8.0.11 · xUnit 2.9 · Moq 4.20.72 · FluentAssertions 6.12.2

## Solution structure
```
SpaceOS.Modules.Abstractions.Domain         ← aggregates, VOs, enums, domain events, service interfaces
SpaceOS.Modules.Abstractions.Application    ← CQRS handlers, validators, IAbstractionsRepository
SpaceOS.Modules.Abstractions.Infrastructure ← EF Core 8 + PostgreSQL, GraphCalculationEngine, TemplateValidator
SpaceOS.Modules.Abstractions.Api            ← Minimal API endpoints (port 5003)
SpaceOS.Modules.Abstractions.Tests          ← xUnit, Moq, FluentAssertions
```

## Layer dependency rule (hard constraint)
```
Domain ← Application ← Infrastructure ← Api
                                       ← Tests
```

## Golden Rules
1. Nincs public setter az aggregate-eken
2. Business logic csak Domain-ben
3. Minden mutáció domain eventet ráz
4. PopDomainEvents() + dispatch minden mutating handler végén
5. Result<T> minden handler return type-ja
6. ConfigureAwait(false) minden production async call-ban
7. AsNoTracking() minden read-only lekérdezésben
8. Math.Round(_, 1, MidpointRounding.AwayFromZero) minden kalkulációban (BE-01)
9. RuleOperator unknown → DomainException (SEC-03)
10. FileReference: regex validáció + nem kezdődhet / vagy ..-tal (SEC-02)
11. Kahn's iteratív BFS — NINCS rekurzív graph traversal (BE-02)
12. ITemplateValidator.Validate() minden template mentés előtt (BE-03)

## Naming conventions
| Scope | Convention |
|---|---|
| Classes, methods, properties | PascalCase |
| Private fields | _camelCase |
| Local variables | camelCase |
| CancellationToken param | always `ct` |
| File name | 1:1 with class name |

## API surface
```
POST   /api/modules/templates                          CreateProductTemplate
GET    /api/modules/templates                          ListProductTemplates
GET    /api/modules/templates/{id}                     GetProductTemplate
GET    /api/modules/templates/{id}/graph               GetTemplateGraph
POST   /api/modules/templates/{id}/slots               AddComponentSlot
POST   /api/modules/templates/{id}/connections         AddSlotConnection
PUT    /api/modules/templates/{id}/parameters/{key}    SetTemplateParameter
POST   /api/modules/templates/{id}/clone               CloneProductTemplate (SEC-05)
POST   /api/modules/templates/{id}/calculate           CalculateProduct
GET    /api/modules/templates/{id}/cutting-list        GetCuttingList (Cache-Control: no-store)
GET    /health                                         Health check (anonymous)
```

## Security
- ManufacturerOnly policy: `tenant_type = "Manufacturer"` claim (all endpoints except /health)
- RLS FORCE on all 5 tables (spaceos_modules schema)
- DAG cycle detection: DB trigger + Kahn's engine check
- FileReference: whitelist extensions (step, stp, ifc, obj, stl, dxf, 3mf)

## KÖTELEZŐ PIPELINE — MINDEN FELADATRA

⚠️ Minden lépés kötelező. Kihagyni TILOS. Lásd teljes leírás: `/opt/spaceos/docs/WORKFLOW.md`

```
INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX
```

### 1. INBOX READ
- `ls ./docs/mailbox/inbox/` → legfrissebb UNREAD üzenet elolvasása
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
- Golden Rules 1–12 teljesülnek?
- Kahn's iteratív BFS (nem rekurzív) a graph traversal?
- `Math.Round(_, 1, MidpointRounding.AwayFromZero)` minden kalkulációban?
- Nincs `TODO`/`FIXME` a kódban?

### 6. SECURITY ⚠️
- **Authorization**: minden endpoint `ManufacturerOnly` policy mögött?
- **RLS**: mind az 5 tábla RLS FORCE-al védve (spaceos_modules schema)?
- **DAG cycle detection**: DB trigger + Kahn engine dupla check?
- **FileReference**: regex validáció + extension whitelist (step/stp/ifc/obj/stl/dxf/3mf)?
- **RuleOperator unknown**: `DomainException`-t dob, nem silent fallback?
- **OWASP Top 10**: nincs nyilvánvaló sebezhetőség?

### 7. OUTBOX ⚠️ SOHA NEM HAGYHATÓ KI
Minden befejezett feladat után kötelező outbox üzenetet írni.
Fájlnév: `YYYY-MM-DD_NNN_[slug]-done.md` → `./docs/mailbox/outbox/`

```markdown
---
id: MSG-AXXX-DONE
from: abstractions
to: conductor
type: done
status: UNREAD
---

## Összefoglaló
[Mit implementáltál, mely fájlok változtak]

## Tesztek
[Hány teszt futott, mind zöld? Új tesztek száma?]

## Security review
[Mely pontokat ellenőrizted (RLS, auth policy, DAG check, FileReference, stb.)]

## Kockázatok / kérdések
[Ha van → status: BLOCKED és leírás]
```

**Ha elakadtál:** `status: BLOCKED` outbox üzenettel jelezz — ne folytasd találgatással.

---

## Közös erőforrások

- **Inbox**: `./docs/mailbox/inbox/` (symlink → `/opt/spaceos/docs/mailbox/abstractions/inbox/`)
- **Outbox**: `./docs/mailbox/outbox/` (symlink → `/opt/spaceos/docs/mailbox/abstractions/outbox/`)
- **Codebase_Status.md**: `./docs/Codebase_Status.md` — frissítsd minden sprint után
- **WORKFLOW.md**: `/opt/spaceos/docs/WORKFLOW.md` — teljes munka módszertan
- **Projekt vízió (üzleti)**: `/opt/spaceos/docs/SpaceOS_Vision_Results_20260413.md` — miért épül a rendszer, Doorstar first customer, célpiac
- **Technikai master overview**: `/opt/spaceos/docs/vision/SpaceOS_Vision_Master.md` — 4 réteg, 5 Golden Rule, domain modell, döntési fa
- **`/spaceos-terminal` skill**: inbox olvasás + build/test gate + DONE/BLOCKED outbox protokoll — `/opt/spaceos/.claude/skills/spaceos-terminal/`
