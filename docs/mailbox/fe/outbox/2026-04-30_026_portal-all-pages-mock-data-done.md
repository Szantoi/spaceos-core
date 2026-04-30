---
id: MSG-FE-026-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-026
created: 2026-04-30
---

## Összefoglaló

FE-026 teljesítve — minden portal oldal mock adatokkal renderelődik.

**Commit:** `69cbc68` — feat: FE-026 — Portal all pages mock data + redesign

**Új fájlok:**
- `src/mocks/ordersMock.ts` — MockOrder[], ORDER_STATUS_TONE, fmtHUF
- `src/mocks/productionMock.ts` — MockCuttingPlan[], NestingPart[], FlowEpic[]
- `src/mocks/settingsMock.ts` — MockUser[], MockAuditEvent[], MockPartner[]
- `src/mocks/shopfloorMock.ts` — MockMachine[], MockShopfloorTask[]
- `src/components/ui/Badge.tsx` — variant rendszer (teal/indigo/amber/emerald/rose/stone/sky)
- `src/components/ui/KpiCard.tsx` — KPI metric kártya
- `src/components/ui/ProgressBar.tsx` — configurable track/fill/height
- `src/components/ui/StatusDot.tsx` — colored dot + pulse animáció

**Módosított oldalak (API fallback stratégia: `displayData = apiData ?? DEMO_DATA`):**
- `OrdersPage.tsx` — DEMO_DOOR_ORDERS 5 rekord, stone/teal palette
- `CuttingPlanListPage.tsx` — DEMO_PLANS 6 rekord, Badge+ProgressBar kártyák
- `CuttingPlanDetailPage.tsx` — DEMO_PLAN_DETAIL 2 lap, SVG nesting vizualizáció
- `ManufacturingFsmBoardPage.tsx` — DEMO_ORDERS 7 rekord, 4 FSM oszlop
- `B2bHandshakesPage.tsx` — DEMO_HANDSHAKES 6 partner, meta adatok
- `AuditLogPage.tsx` — DEMO_AUDIT 8 esemény, event tone színek
- `UserListPage.tsx` — DEMO_USERS 6 user, avatar initials, role badge-ek
- `ShopFloorTaskListPage.tsx` — DEMO_TASKS_RESPONSE 4 feladat, kiosk dark UI

**Stratégia:** Minden oldal megtartja a loading/error testid-eket. A mock adat csak akkor jelenik meg, ha az API nem ad vissza adatot (isLoading=false, apiData=undefined). Tesztek nem érintettek — az API layer mockolva van, a valódi API adatok felülírják a demo adatokat.

## Tesztek

**242 / 242 pass** — 0 új teszt (meglévők változatlanul zöldek)

```
Test Files  44 passed (44)
      Tests  242 passed (242)
   Duration  33.54s
```

## Security review

- InMemoryWebStorage megőrizve — nincs sessionStorage/localStorage
- Nincs `dangerouslySetInnerHTML` — minden user content escaped
- Auth guard érintetlen — WorldGuard nem módosult
- sourcemap: false production build-ben (vite.config.ts változatlan)
- Token nem logolható — console.log nem került be

## Kockázatok / kérdések

Nincsenek.

Demo adatok statikusan beégetve a komponens fájlokba. Amikor a BFF élesen fut, az `apiData ?? DEMO_DATA` pattern az API adatot részesíti előnyben — a demo adatok nem látszanak production módban valódi backendnél.
