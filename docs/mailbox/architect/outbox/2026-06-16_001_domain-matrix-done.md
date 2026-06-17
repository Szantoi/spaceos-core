---
id: MSG-ARCH-001-DONE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-001
created: 2026-06-16
---

## FE Domain Ownership Matrix v1 — Összefoglaló

A Slice 1 scope 19 page-t fed le, amelyeken a backend már deployolva van és csak a FE mock-réteg cserélendő valós API hívásra: ez elsősorban 10 PARTIAL oldal mock fallback-jének eltávolítását jelenti (OrdersPage, WorkflowPage, SalesPage, InventoryPage, ProcurementPage, ProductionPage, AnalyticsPage, DashboardPage, SettingsPage, DesignPage), kiegészítve 9 közvetlen bekötéssel meglévő backendre (warehouse aloldalak, SupervisorPage, AiPage, MasterdataPage, ProductionDashboardPage) — becsült scope 2-3 sprint.

Összesen 5 hiányzó backend modul lett azonosítva: **CRM** (5010), **Finance** (5011), **Project** (5012), **Maintenance** (5013) és **HR/Attendance/EHS** (5014) — ezek nélkül 13 page marad MOCK, és csak a Slice 2 fázisban integrálhatók; mindegyikhez külön architekturális spec és arch-planner pipeline szükséges a kiadás előtt.

A javasolt FE terminál domain split 4 önálló terminálra bontja a jelenlegi FE-A/FE-B párhuzamos worktree-megközelítést: **FE-CORE** (Kernel/Joinery/Cutting/Abstractions — Dashboard, Orders, Workflow, Production, Settings, Design), **FE-SALES** (Sales/CRM/Finance/Projects — Sales, CRM, Finance, Projects, Interior), **FE-OPS** (Inventory/Procurement/Cutting analitika — Inventory, Procurement, Analytics, MasterData, warehouse/*), **FE-PEOPLE** (HR/AI/Tasks/Docs — HR, Attendance, EHS, Maintenance, AI, Tasks, Docs); FE-CORE + FE-SALES + FE-OPS párhuzamosan indítható a Slice 1-ben, FE-PEOPLE csak Slice 2 backend modulok után.
