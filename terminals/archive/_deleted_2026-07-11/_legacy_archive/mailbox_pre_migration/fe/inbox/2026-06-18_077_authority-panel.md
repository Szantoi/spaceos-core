---
id: MSG-FE-077
from: root
to: fe
type: task
priority: high
status: READ
model: sonnet
ref: MSG-FE-076
created: 2026-06-18
---

# FE-077 — Hatáskör-mátrix (AuthorityPanel) implementálás

## Feladat

Implementáld a Beállítások → Hatáskörök panel bővítését a `page-auth.jsx` terv alapján.

## Scope

A hatáskör-mátrix rendszer:
1. **Jóváhagyási küszöbök szerkesztése** — PO érték, sztornó limit, kedvezmény %, túlóra
2. **Jóváhagyási kérelmek kezelése** — fuggoben → jovahagyva / elutasitva FSM
3. **Perm-védelem** — `settings.manage` a küszöbökhöz, `auth.approve` a döntésekhez

## Meglévő alapok

A store már tartalmazza (ellenőrizd):
- `authConfig = { poValue, voidValue, discountPct, overtimeHours }`
- `approvals[]` tömb FSM-mel
- `setAuthConfig(patch)`, `decideApproval(id, approve, {reason})`
- `AUTH_ACTIONS`, `AUTH_STATUS` konstansok

## DoD

- [ ] `AuthorityPanel` komponens (Beállítások fül)
- [ ] Küszöb-szerkesztő 3 oszlopos grid
- [ ] Kérelmek listája (pending + decided)
- [ ] Jóváhagy/Elutasít gombok `auth.approve` joggal
- [ ] Elutasításnál indoklás kötelező
- [ ] Perm-hiánynál lezárt gomb + tooltip
- [ ] `npm run build` sikeres
- [ ] DONE outbox

## Ref

Terv: `docs/tasks/new/joinerytech/page-auth.jsx` (103 sor)

---

Timestamp: 2026-06-18 06:22 UTC
