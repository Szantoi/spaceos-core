---
id: SPEC-FE-DOMAIN-001
source: /opt/spaceos/docs/mailbox/architect/outbox/2026-06-16_001_domain-matrix-done.md
type: Architecture spec (Organizational + UI domain structure)
scope: [fe, kernel, joinery, cutting, abstractions, sales, inventory, procurement]
priority: high
complexity: 4
dependencies: ["Slice 1 backend deployment complete", "Mock-to-API migration strategy"]
status: NEW
created: 2026-06-17
---

# FE Domain Ownership Matrix v1

## Összefoglaló
Az Architect a Slice 1 FE scope-ot 4 önálló terminálra szétválasztja (FE-CORE, FE-SALES, FE-OPS, FE-PEOPLE) és azonosítja a 10 PARTIAL page mock fallback eltávolítási szükségletét, valamint 5 Slice 2 backend modul hiányát (CRM, Finance, Project, Maintenance, HR/EHS) amely 13 oldalt blokk.

## Scope

### Érintett terminálok
- **FE-CORE** (új) — Kernel/Joinery/Cutting/Abstractions backend-ekhez
- **FE-SALES** (új) — Sales/CRM/Finance/Projects backend-ekhez
- **FE-OPS** (új) — Inventory/Procurement/Cutting analytics backend-ekhez
- **FE-PEOPLE** (új) — HR/AI/Tasks/Docs backend-ekhez
- Érintett backend modulok: Kernel, Joinery, Cutting, Abstractions, Sales, Inventory, Procurement

### Slice 1 FE integrációs igény
- **19 page** Slice 1 scope-ban
  - **10 PARTIAL oldal:** OrdersPage, WorkflowPage, SalesPage, InventoryPage, ProcurementPage, ProductionPage, AnalyticsPage, DashboardPage, SettingsPage, DesignPage
  - **9 közvetlen oldal:** warehouse aloldalak, SupervisorPage, AiPage, MasterdataPage, ProductionDashboardPage
- **Becsült scope:** 2-3 sprint
- **Blokkolt:** 13 page a 5 hiányzó backend modul miatt

### Slice 2 backend modulok (hiányzó)
1. **CRM** (5010) — Sales/CRM pages
2. **Finance** (5011) — Finance pages
3. **Project** (5012) — Projects pages
4. **Maintenance** (5013) — Maintenance pages
5. **HR/Attendance/EHS** (5014) — HR/Attendance/EHS/People pages

## Implementációs javaslat

### 1. FE Terminál Szétválasztás (Slice 1 előtt)
- **FE-CORE:** Dashboard, Orders, Workflow, Production, Settings, Design
  - Backend: Kernel, Joinery, Cutting, Abstractions
  - Párhuzamosan indítható

- **FE-SALES:** Sales, CRM, Finance, Projects, Interior
  - Backend: Sales, (CRM, Finance, Projects → Slice 2)
  - Párhuzamosan indítható

- **FE-OPS:** Inventory, Procurement, Analytics, MasterData, warehouse/*
  - Backend: Inventory, Procurement
  - Párhuzamosan indítható

- **FE-PEOPLE:** HR, Attendance, EHS, Maintenance, AI, Tasks, Docs
  - Backend: HR/Attendance/EHS, Maintenance (Slice 2+)
  - **Indítható csak Slice 2 után**

### 2. Mock-to-API Migráció Sorrendje (Slice 1)
1. FE-CORE terminál: 10 PARTIAL page mock fallback eltávolítása
2. FE-SALES terminál: Sales mock fallback eltávolítása (CRM/Finance/Projects továbbra is MOCK)
3. FE-OPS terminál: Inventory/Procurement API integrációk

### 3. Slice 2 Megelőző Munka
- Mindegyik 5 backend modulhoz (CRM, Finance, Project, Maintenance, HR/EHS):
  - Architekturális spec (Architect)
  - API contract design (Orch/Module)
  - FE stub komponensek (FE-SALES / FE-PEOPLE)

## Kockázatok

### Technikai kockázatok
1. **Terminál splitmeg:** FE-A/FE-B worktree zavarok → szükséges clear merge strategy
2. **Mock fallback eltávolítás:** Hiányos backend API behavior → elvezethet FE hibákhoz
3. **Slice 2 gépesítés:** 5 backend modul párhuzamos spec-zése → koordinációs terhelés

### Függőségek
- Backend deployment (Kernel, Sales, Joinery, Cutting, Inventory, Procurement) DONE kell
- API contract jól definiálva (OpenAPI spec minden modulhoz)
- FE mock fallback eltávolítási sorrendje meghatározva

## Ajánlott döntések

1. **FE-CORE indítás:** 2-3 sprinten belül (legalacsonyabb kockázat)
2. **FE-SALES indítás:** Párhuzamosan FE-CORE-ral (CRM/Finance/Projects továbbra is MOCK)
3. **FE-OPS indítás:** Párhuzamosan, Inventory/Procurement API-k után
4. **FE-PEOPLE** indítás:** **Csak** Slice 2 backend modulok után
5. **Architect spec-pipeline:** Parallel CRM, Finance, Project, Maintenance, HR/EHS specifikációhoz

## Eredeti dokumentum
Source: `/opt/spaceos/docs/mailbox/architect/outbox/2026-06-16_001_domain-matrix-done.md`
