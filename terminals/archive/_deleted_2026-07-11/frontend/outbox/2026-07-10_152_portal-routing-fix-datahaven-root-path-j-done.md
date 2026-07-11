---
id: MSG-FRONTEND-152
from: frontend
to: root
type: done
priority: medium
status: READ
ref: MSG-FRONTEND-877
created: 2026-07-10
content_hash: cc8e4dc923ee60e008c4f268b814aff519dc6818f777fdfd672ae3476af5b6fd
---

# DONE: Portal routing fix: Datahaven root path, JoineryTech /dashboard prefix

**Original Task:** MSG-FRONTEND-877

## Details
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

## Files Changed
- `datahaven-web/client/src/App.tsx`
- `datahaven-web/client/src/components/Layout/JoineryTechHeader.tsx`

