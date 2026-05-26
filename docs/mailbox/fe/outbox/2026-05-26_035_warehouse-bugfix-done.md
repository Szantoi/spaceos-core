---
id: MSG-FE-035-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-035
created: 2026-05-26
---

# FE-035 DONE — Raktár bugfix: embedded Mozgások + Beszerzés

## Státusz: KÉSZ

## Elvégzett munkák

### Bug 1 fix — `MovementsPage` embedded prop
**`src/pages/warehouse/MovementsPage.tsx`:**
- `MovementsPageProps { embedded?: boolean }` interface hozzáadva
- `embedded=true` esetén: `space-y-4` wrapper (px-7 py-6 nélkül)
- Summary cards (`grid grid-cols-2 md:grid-cols-4`) csak `!embedded` esetén jelenik meg
- Filter bar és timeline minden esetben látható

**`src/pages/InventoryPage.tsx`:**
- `<MovementsTab />` → `<MovementsTab embedded />` (duplázott padding megszűnt)

### Bug 2 fix — `ProcurementPage` layout
**`src/pages/ProcurementPage.tsx`:**
- `px-7 py-6 max-w-[1400px]` → `w-full px-7 py-6 max-w-[1400px]`
- A route (`/w/warehouse/procurement`) helyesnek bizonyult, a tesztek megerősítik

### `src/__tests__/App.test.tsx` — warehouse route tesztek
3 új teszt:
- `renders warehouse overview` → `/w/warehouse` → "Raktár" megjelenik
- `renders warehouse procurement screen` → "Aktív megrendelések" megjelenik
- `renders warehouse movements screen` → "Anyagmozgás napló" megjelenik

## Tesztek
- **247 teszt, 0 fail** (volt 244, +3 új warehouse route teszt)
- `pnpm build` → 0 error
- `pnpm lint` → 0 új hiba
