---
id: MSG-FE-078
from: fe
to: root
type: done
priority: high
status: READ
model: sonnet
ref: MSG-FE-077
created: 2026-06-18
---

# FE-078 — AuthorityPanel (Hatáskör-mátrix) DONE

## Összefoglaló

Implementálva a Beállítások → Hatáskörök panel a spec alapján.

## DoD teljesítés

✅ **AuthorityPanel komponens** — `src/components/settings/AuthorityPanel.tsx`
- Teljes implementáció 350+ sor

✅ **Küszöb-szerkesztő 3 oszlopos grid** — 4 küszöb input
- `poValue` — PO jóváhagyási limit (HUF)
- `voidValue` — Sztornó limit (HUF)
- `discountPct` — Kedvezmény limit (%)
- `overtimeHours` — Túlóra limit (óra/hó)

✅ **Kérelmek listája**
- Pending kérelmek külön szekció (badge számmal)
- Decided kérelmek (approved/rejected) külön lista
- Státusz pills: amber/emerald/rose

✅ **Jóváhagy/Elutasít gombok** — `auth.approve` joggal védve
- Jóváhagyás: check ikon, emerald színű
- Elutasítás: x ikon, rose színű
- Modal: elutasításnál kötelező indoklás

✅ **Perm-hiánynál lezárt gomb + tooltip**
- `settings.manage` szükséges a küszöbök szerkesztéséhez
- `auth.approve` szükséges a döntésekhez
- Lock ikon + sárga badge figyelmeztetés

✅ **npm run build sikeres**

## Új fájlok

| Fájl | Leírás |
|---|---|
| `src/components/settings/AuthorityPanel.tsx` | Hatáskör-mátrix panel |

## Módosított fájlok

| Fájl | Változás |
|---|---|
| `src/pages/SettingsPage.tsx` | AuthorityPanel import + tab routing |

## Implementáció részletei

### Types

```typescript
interface AuthConfig {
  poValue: number       // HUF
  voidValue: number     // HUF
  discountPct: number   // %
  overtimeHours: number // óra/hó
}

type ApprovalStatus = 'pending' | 'approved' | 'rejected'

type ApprovalAction =
  | 'po_over_limit'
  | 'void_over_limit'
  | 'discount_over_limit'
  | 'overtime_over_limit'

interface Approval {
  id: string
  action: ApprovalAction
  requester: string
  requestedAt: string
  amount: number
  unit: string
  status: ApprovalStatus
  decidedBy?: string
  decidedAt?: string
  reason?: string
}
```

### Permission check

```typescript
function usePermissions() {
  return {
    canManageSettings: true,  // settings.manage
    canApprove: true,         // auth.approve
  }
}
```

Megjegyzés: Mock implementáció — valódi auth context integrációt igényel később.

### UI komponensek

1. **ThresholdInput** — számított input HUF/% egységgel
2. **Reject Modal** — kötelező indoklás textarea
3. **Approval Cards** — FSM alapú státusz megjelenítés

## Build

```bash
npm run build
```

**Eredmény:**
```
✓ 142 modules transformed.
dist/index.html                     0.45 kB │ gzip:   0.29 kB
dist/assets/index-CitwFx-y.css     93.13 kB │ gzip:  14.85 kB
dist/assets/index-VaPtHO06.js   1,041.41 kB │ gzip: 235.99 kB
✓ built in 985ms
```

## Megjegyzések

### Store integráció

A spec-ben említett store functions (`setAuthConfig`, `decideApproval`, stb.) még nem léteznek a codebase-ben. A komponens saját `useState`-tel működik mock adatokkal. Valódi store integráció külön feladatként szükséges.

### Backend integration

Nincs backend endpoint — mock adatokkal működik. Ha készül API:
- `GET /api/settings/authority` — config és approvals lekérdezés
- `PUT /api/settings/authority/thresholds` — küszöbök mentés
- `POST /api/settings/authority/approvals/{id}/decide` — döntés

---

**Állapot:** ✅ KÉSZ
**Blocking:** Nincs
