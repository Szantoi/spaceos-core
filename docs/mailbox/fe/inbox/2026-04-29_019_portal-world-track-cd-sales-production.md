---
id: MSG-FE-019
from: root
to: fe
type: task
priority: high
status: READ
ref: MSG-FE-018-DONE
created: 2026-04-29
---

# FE-019 — Portal World Track B+C+D: Home + Sales + Production (Day 4–13)

> **Tervdok:** `docs/tasks/active/SpaceOS_Portal_World_Architecture_v4_final.md` — Section 6-8
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** FE-018 ✅ (136 teszt, Track A Foundation)
> **ORCH-085 (Track F) párhuzamosan fut — BFF route-ok mock-olhatók MSW-vel**
> **Használhatsz sub-agent-eket** ha szükséges

---

## Track B: Home + Settings (~2 nap)

### WorldHome (/)

- Auto-redirect a tenant default world-jébe (Sales ha van order modul, Production ha cutting)
- WorldCard grid: 5 world kártya, enabledModules alapján szűrve

### Settings (/w/settings/)

- TenantInfo page — tenant név, tier, enabledModules display
- AuditLog page — `/bff/api/audit` hívás, paginated tábla
- UserList page — `/bff/api/users` hívás, role display

---

## Track C: Sales (/w/sales/) (~5 nap)

### OrdersList

- TanStack Query: `useQuery(['orders'], ...)`
- PagedTable, filter (status, date), search
- "Új rendelés" gomb → Create flow

### OrderDetail

- `/bff/api/orders/:id/full` aggregátor hívás (ha BFF kész) VAGY `/bff/joinery/orders/:id`
- Tételek listája, státusz badge, akciógombok (Submit, Calculate, Cancel)
- **Dokumentumok szekció:** BatchPdfButton + AnyaglistaButton (FE-012-ből reuse!)

### ProductConfigurator (ha van Abstractions endpoint)

- `/bff/api/configurator/:productTemplateId/configure`
- Parametrikus form (react-hook-form + zod schema)
- **CONTRACT_ISSUES:** Ha a BFF-01 fix nincs kész, a meglévő `/bff/abstractions/*` route-ot használd

### B2bHandshakes

- Handshake lista + detail
- Cross-tenant partner connection display

---

## Track D: Production (/w/production/) (~5 nap)

### CuttingPlanList

- `/bff/cutting/plans` hívás
- Filter: status, date, machine
- Kártya nézet + tábla nézet toggle

### CuttingPlanDetail

- `/bff/cutting/plans/:id/full` aggregátor hívás
- Nesting vizualizáció (SVG — FreeTier Portal SheetSvg component reuse!)
- Sheet-ek + placement-ek display
- Execution státusz

### ManufacturingFsmBoard (heavy screen!)

- `/bff/manufacturing/orders` hívás
- Kanban-style FSM board: Pending → EdgeBanding → CNC → Complete
- Drag-and-drop NINCS v1-ben (klikk-alapú state transition)
- Task detail modal (EdgeBanding / CNC task info)

### TaskDetail

- Manufacturing task részletek
- Worker assignment info
- Progress events lista

---

## Tesztek (80+)

**Track B (15+):** WorldHome redirect, WorldCard, Settings pages
**Track C (35+):** OrdersList filter, OrderDetail, Configurator form, Handshakes
**Track D (30+):** CuttingPlanList, PlanDetail SVG, ManufacturingFsmBoard, TaskDetail

## Definition of Done

- [ ] WorldHome auto-redirect + WorldCard grid
- [ ] Settings: TenantInfo + AuditLog + UserList
- [ ] Sales: OrdersList + OrderDetail + Configurator + Handshakes
- [ ] Production: CuttingPlanList + PlanDetail + ManufacturingFsmBoard + TaskDetail
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 216 pass (136 + 80 új)
- [ ] `pnpm lint` 0 error
- [ ] Outbox DONE
