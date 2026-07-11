---
id: MSG-ARCHITECT-039
from: conductor
to: architect
type: task
priority: high
status: READ
injected: 2026-07-01
model: opus
epic_id: EPIC-JT-MAINT
created: 2026-07-01
content_hash: 1589f56a3e14b8f0d75f92f8ec1537af49d82b96966084fd0980abc95da862e4
---

# Maintenance Domain Model

## Feladat

Tervezd meg a Maintenance modul domain modelljét a JoineryTech ERP részére.

## Forrás Dokumentáció

- `/opt/spaceos/docs/joinerytech/CLAUDE.md` — Maintenance világ részletes leírása (Eszköz, Munkalap FSM, Megelőző tervek)

## Domain Részletek

**Maintenance Világ:**
- **Eszköz-törzs:** Asset registry (gép, szerszám)
- **Munkalap FSM:** Scheduled → In Progress → Completed/Cancelled
- **Megelőző tervek:** Időköz-alapú (pl. 90 nap) + üzemóra-alapú
- **Állásidő-napló:** Downtime tracking

## Architektúrális Követelmények

1. **Aggregate roots:** Asset, WorkOrder, MaintenancePlan
2. **FSM:** WorkOrder státuszgép
3. **Scheduler:** Megelőző karbantartás trigger logic
4. **Integration:** Maintenance → Production (állásidő hatása gyártásra)

## Kimeneti Dokumentáció

ADR-stílusú design doc:
- Aggregate boundaries (Asset, WorkOrder, MaintenancePlan)
- FSM diagram (WorkOrder)
- Scheduler logic (preventive maintenance)
- Integration contract Maintenance→Production

## Tech Stack

.NET 8 + EF Core + PostgreSQL + scheduler (background service)



## Acceptance Criteria

- [ ] Aggregate boundaries (Asset, WorkOrder, MaintenancePlan)
- [ ] FSM diagram (WorkOrder)
- [ ] Scheduler logic design
- [ ] Integration contract Maintenance→Production
- [ ] ADR dokumentáció
