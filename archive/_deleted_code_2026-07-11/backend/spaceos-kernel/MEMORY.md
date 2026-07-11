# KERNEL Memory

Utolsó frissítés: 2026-06-18

## Aktuális állapot

**Branch:** develop (12 commits ahead of origin/develop)
**Legutolsó commit:** c70a359 — feat(internal): GET /api/internal/tenants/{id} (MSG-KERNEL-107)
**Build:** ✅ 0 errors (60 xUnit warnings - ConfigureAwait/CancellationToken style)
**Tests:** ✅ 1186/1190 passed (4 skipped)
- Unit: 971 passed
- Integration: 108 passed
- API: 107 passed, 4 skipped

**Inbox:** Üres — nincs aktív feladat
**Outbox:** 2026-06-18_108 (BLOCKED) — Bakery visszavonva (MSG-KERNEL-110: ROOT megerősítette ADR-024)

**Modified files (uncommitted):**
- `.claude/skills/spaceos-terminal/SKILL.md` — dokumentáció frissítés
- `CLAUDE.md` — memory fájl hivatkozás + conductor címzett
- `SpaceOS.Kernel.Domain/CLAUDE.md` — backend checklist hozzáadva
- `MEMORY.md` — újonnan létrehozott

## Fontos kontextus

### Zárolt ADR-ek (MSG-KERNEL-108, 2026-06-16)

**ADR-024 — IParametricProduct**
- Kernel **nem tudja és nem kell tudnia** mi egy termék konkrétan
- Kernel csak `IParametricProduct` interfészen dolgozik
- Termékspecifikus logika → Joinery/MEP/Cutting Driver-ekben
- Ha Kernel kódba kerülne termékspecifikus logika → BLOCKED outbox

**ADR-039 — Cross-modul kommunikáció**
- Kernel nem hív közvetlenül Joinery/Cutting/Inventory DB-t
- Modulok csak publikus API-n keresztül kommunikálnak
- Cross-modul adat → Orchestrator BFF koordinál

**ADR-018/019/020 — Immutability & Audit**
- Audit eventek: append-only (nincs UPDATE)
- Tenant adat: RLS-sel izolált
- Ezek nem változtathatók jövőbeli feladatokban sem

### RLS architektúra
- Kernel **NEM** GUC-alapú (`app.current_tenant_id`-t nem használja)
- `IgnoreQueryFilters()` + explicit `WHERE tenantId = ...` minta
- Szándékos architektúrális döntés (lásd CLAUDE.md)

### Internal DELETE endpoint pattern
```
DELETE /api/internal/<resource>/by-tenant?tenantId={guid}&confirm=true
Header: X-SpaceOS-Internal: true
```
4-gate security: internal header + query param + confirmation + tenantId validation

### FlowEpic létrehozási sorrend (KÖTELEZŐ)
```
POST /api/tenants/{tenantId}/facilities → facilityId
POST /api/facilities/{facilityId}/flow-epics + { title } → epicId
```
Direkt `POST /api/flow-epics` nem létezik.

## Következő lépések
1. Várni UNREAD inbox üzenetre (`docs/mailbox/kernel/inbox/`)
2. Ha nincs feladat → READY állapot

## Megoldott problémák

**MSG-KERNEL-109** (2026-06-18) — Bakery Project BLOCKED → visszavonva ✓
- BLOCKED outbox írva ADR-024 alapján (termékspecifikus logika nem Kernel-be való)
- ROOT megerősítette: helyes értékelés (MSG-KERNEL-110)
- Feladat visszakerült docs/tasks/new/-ba, ARCHITECT hatásköre

**MSG-KERNEL-107** (2026-05-28) — ADR-039 internal tenant endpoint
- GET /api/internal/tenants/{id} implementálva
- Actor directory lookup (name, email, emailHash)
- Internal endpoint security pattern alkalmazva

## Session tapasztalatok

- CLAUDE.md frissült: memory fájl hivatkozás + `to: conductor` a DONE outbox-ban
- Domain CLAUDE.md: backend implementációs checklist hozzáadva
- Új xUnit warnings: ConfigureAwait(false) és TestContext.Current.CancellationToken használat
  - Ezek stílus-figyelmeztetések, nem blokkolják a build-et
  - Opcionálisan javítható ha van ráérő időnk vagy ha explicit kérés érkezik
