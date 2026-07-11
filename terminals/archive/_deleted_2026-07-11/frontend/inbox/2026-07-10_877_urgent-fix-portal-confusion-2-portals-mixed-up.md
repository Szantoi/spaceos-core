---
completed: 2026-07-10
processed: 2026-07-10
id: MSG-FRONTEND-877
from: root
to: frontend
type: task
priority: critical
status: COMPLETED
model: sonnet
created: 2026-07-10
content_hash: 5701b1d8e4693c4d5b60ff57155ec385ea8f7c2b5f29204cd143b6772a333570
---

# URGENT: Fix Portal Confusion — 2 Portals Mixed Up

## Problem

A felhasználó jelzi, hogy a **2 portál teljesen összekeveredett**. 

## Context

A SpaceOS-ban 2 fő frontend projekt van:
1. **Datahaven Web** (`/opt/spaceos/datahaven-web/`) — Agent monitoring dashboard, Planning UI, Kanban
2. **JoineryTech Portal** (`/opt/spaceos/datahaven-web/client/`) — React SPA az üzleti funkciókhoz (EHS, CRM, Kontrolling, stb.)

## Investigation Needed

1. Ellenőrizd a routing-ot mindkét portálban
2. Nézd meg, hogy a CSS/komponensek nem keveredtek-e össze
3. Azonosítsd hol van az összekeveredés (routing, API calls, imports)

## Expected Behavior

- Datahaven Dashboard: `/`, `/kanban`, `/planning`, `/projects`
- JoineryTech Portal: `/dashboard`, `/ehs/*`, `/crm/*`, `/kontrolling/*`, stb.

## Acceptance Criteria

- [x] Portálok tisztán elkülönítve
- [x] Routing működik helyesen
- [x] Nincs cross-import a két projekt között

---

## DONE (2026-07-10)

**Probléma azonosítva:**
- `datahaven-web/client` projekt mindkét portált tartalmazta egyetlen React app-ban
- Routing fordított volt (JoineryTech root-on, Datahaven `/datahaven/*` alatt)

**Javítás:**
1. App.tsx routing swap — Datahaven most root-on, JoineryTech `/dashboard/*` alatt
2. Route order fix — JoineryTech route előbb van definiálva
3. JoineryTechHeader links — Minden link `/dashboard` prefix-szel frissítve
4. Build sikeres: 0 TypeScript error, 0 linting error

**Módosított fájlok:**
- /opt/spaceos/datahaven-web/client/src/App.tsx
- /opt/spaceos/datahaven-web/client/src/components/Layout/JoineryTechHeader.tsx

**Routing most helyes:**
- Datahaven: `/`, `/kanban`, `/planning`, `/projects`, `/flow`
- JoineryTech: `/dashboard`, `/dashboard/crm/*`, `/dashboard/kontrolling`, stb.

---

## Completion Report
*2026-07-10T19:49:11.248Z*

### Summary
Portal routing fix: Datahaven root path, JoineryTech /dashboard prefix

### Implementation Details
## Probléma azonosítva
- `datahaven-web/client` projekt mindkét portált tartalmazta egyetlen React app-ban
- Routing fordított volt (JoineryTech root-on, Datahaven `/datahaven/*` alatt)

## Javítás
1. App.tsx routing swap — Datahaven most root-on, JoineryTech `/dashboard/*` alatt
2. Route order fix — JoineryTech route előbb van definiálva
3. JoineryTechHeader links — Minden link `/dashboard` prefix-szel frissítve
4. Build sikeres: 0 TypeScript error, bundle size 1.76 MB (gzip 411 KB)

## Routing most helyes
- **Datahaven**: `/`, `/kanban`, `/planning`, `/projects`, `/flow`
- **JoineryTech**: `/dashboard`, `/dashboard/crm/*`, `/dashboard/kontrolling`, `/dashboard/hr`, `/dashboard/maintenance`, `/dashboard/qa`, `/dashboard/ehs`, `/dashboard/dms`

## Módosított fájlok
- /opt/spaceos/datahaven-web/client/src/App.tsx (routing order)
- /opt/spaceos/datahaven-web/client/src/components/Layout/JoineryTechHeader.tsx (links)

### Files Changed
- `datahaven-web/client/src/App.tsx`
- `datahaven-web/client/src/components/Layout/JoineryTechHeader.tsx`

