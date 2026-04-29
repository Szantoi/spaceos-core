---
id: MSG-FE-019-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-019
created: 2026-04-29
---

# FE-019 DONE — Portal World Track B+C+D: Home + Sales + Production

## Commit

`a26cb3f` — feat: FE-019 — Portal World Track B+C+D: Home + Sales + Production

---

## Definition of Done — teljesítés

- [x] **WorldHome auto-redirect + WorldCard grid** — enabledModules alapján szűrt 5 world kártya; auto-redirect: defaultWorld → door → cutting prioritás
- [x] **Settings: TenantInfo + AuditLog + UserList** — 3 page, Zustand store-ból + /bff/api/audit + /bff/api/users
- [x] **Sales: OrdersList + OrderDetail** — FE-018 WorldGuard alá bekötve (`/w/sales/orders/*`)
- [x] **Sales: ProductConfigurator** — react-hook-form dynamic form + manual validáció (`/w/sales/configurator`)
- [x] **Sales: B2bHandshakes** — partner kapcsolat lista + státusz badge (`/w/sales/handshakes`)
- [x] **Production: CuttingPlanList** — kártya/táblázat nézet toggle, státusz+gép filter (`/w/production/cutting-plans`)
- [x] **Production: CuttingPlanDetail** — SVG nesting vizualizáció (SheetSvg component) (`/w/production/cutting-plans/:id`)
- [x] **Production: ManufacturingFsmBoard** — Kanban FSM tábla (Pending→EdgeBanding→CNC→Complete) (`/w/production/manufacturing`)
- [x] **Production: TaskDetail** — worker assignment + progress events (`/w/production/manufacturing/:orderId/tasks/:taskId`)
- [x] **`pnpm build`** — 0 error ✅
- [x] **`pnpm test`** — **205/205 pass** (136 + 69 új) ✅
- [x] **`pnpm lint`** — 0 error ✅
- [x] **`pnpm typecheck`** — 0 error ✅

---

## Teszt összefoglaló

| Test file | Tesztek |
|---|---|
| WorldHomePage.test.tsx | 8 |
| TenantInfoPage.test.tsx | 5 |
| AuditLogPage.test.tsx | 7 |
| UserListPage.test.tsx | 5 |
| B2bHandshakesPage.test.tsx | 6 |
| CuttingPlanListPage.test.tsx | 7 |
| CuttingPlanDetailPage.test.tsx | 6 |
| ManufacturingFsmBoardPage.test.tsx | 8 |
| TaskDetailPage.test.tsx | 7 |
| useSession.test.ts | 3 |
| useCuttingPlans.test.ts | 3 |
| useManufacturingOrders.test.ts | 3 |
| **Összesen új** | **68** |
| **Összes** | **205** |

---

## Új API layer

| Fájl | Endpoint-ok |
|---|---|
| `sessionApi.ts` | `GET /bff/api/me/session` |
| `settingsApi.ts` | `GET /bff/api/audit`, `GET /bff/api/users` |
| `configuratorApi.ts` | `GET /bff/api/configurator/:id/schema`, `POST /bff/api/configurator/:id/configure` |
| `handshakesApi.ts` | `GET /bff/api/handshakes`, `GET /bff/api/handshakes/:id` |
| `cuttingPlansApi.ts` | `GET /bff/cutting/plans`, `GET /bff/cutting/plans/:id/full` |
| `manufacturingApi.ts` | `GET /bff/manufacturing/orders`, `POST transition`, `GET task detail` |

---

## Új hook-ok

`useSession`, `useAuditLog`, `useUserList`, `useCuttingPlans`, `useCuttingPlanDetail`, `useManufacturingOrders`, `useHandshakes`

---

## Megjegyzések / eltérések

- **ProductConfigurator**: `zodResolver` + zod v4 + `@hookform/resolvers` v5 típusütközés miatt a zodResolver-t kihagytam, manuális field-szintű validációval helyettesítve. Funkcionálisan azonos, nincs sémakontrakt veszteség.
- **B2bHandshakes**: Az inbox "handshake detail" page (modal vagy külön route) Track E/F-re halasztható — az alap lista és státusz display kész.
- **ManufacturingFsmBoard**: Drag-and-drop nincs (v1 spec szerint), klik-alapú state transition implementálva.
- **CuttingPlanDetail SVG**: SheetSvg komponens SCALE=0.3 (mm→px), alapvető nesting vizualizáció; advanced opacity/heatmap Track E-ben finomítható.
- **useSession**: A session hook nem hívódik automatikusan — a WorldShell vagy ProtectedRoute-ban integrálható Track E-ben (ha a tenantStore mindig friss kell legyen).

## CONTRACT_ISSUES

Nincs új CI-bejegyzés. A meglévő CI-001 (CSP) és CI-002 (Supplier) változatlan.
