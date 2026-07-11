---
processed: 2026-07-10
id: MSG-BACKEND-194
from: root
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-ROOT-040
epic_id: EPIC-DOORSTAR-SOFTLAUNCH
created: 2026-07-08
content_hash: 4a430828f89d8696a92ec7fb1e3eff9078c8cd50e2d733fa80187807630b84bb
---

# Doorstar Production Workflow Tracking — Implementation Plan

## Context

**Ügyfél**: Doorstar Kft. (valós éles ügyfél, EPIC-DOORSTAR-SOFTLAUNCH, target 2026-09-30)

**Probléma**: Műhely-státusz követés ma papír-kanban (Munkamenet.pdf kinyomtatva, kézzel jelölve, Viber-fotó a tulajnak). Nincs valós idejű láthatóság, csúszások, koordinációs káosz.

**Megoldás**: SpaceOS Production Workflow Tracking modul (Layer 2 DRIVER) - mobil-first UI műhelyvezetőknek, élő státusz push tulajnak/sales-nek.

## Cabinet-VPS Hybrid Workflow State

### ✅ DONE - Domain Spec (Cabinet)
- **MSG-ROOT-038**: Doorstar domain spec v1 (valós Munkamenet.pdf alapján korrigálva)
- **MSG-ROOT-040**: Cabinet validáció + Gábor döntés - 6 STAGE végleges scope
- **MSG-CABINET-BRIDGE-019**: VPS architektúra javaslat (Layer 2, FSM, event integráció)

### ✅ ELFOGADVA - Architecture
- **Layer 2 DRIVER**: `spaceos-modules-production` (.NET 8, DDD/CQRS/FSM)
- **2-szintű FSM**:
  - `ProductionJob.Status` (aggregate) - Queued → Cutting → Preparation → Assembly → Packaging → ShippingReady
  - `WorkflowStep.Status` (6 STAGE) - Queued → InProgress → Done
- **Event integráció**:
  - Bejövő: `CuttingJob.CuttingCompleted` (ADR-038), `OrderItem.OrderConfirmed`
  - Kimenő: `ProductionJob.ShippingReady` → Sales/tulaj push (Viber kiváltás)

### ⏳ KÖVETKEZŐ - Implementation Plan (Backend)

Cabinet kéri: **OpenAPI contract draft + Implementation Plan** (backend/frontend task breakdown, timeline, integration points).

## A 6 STAGE (végleges scope)

| # | STAGE | Lefedi a Munkamenet-fázisok közül | Trigger | Mobil UI |
|---|-------|-----------------------------------|---------|----------|
| 1 | **Szabászat/Előgyártás** | Szabás, 22-es marás, HDF keret, üvegezés-előkészítés | Auto: `CuttingJob.CuttingCompleted` | Auto sárga→zöld |
| 2 | **Megmunkálás** | CNC kontúrmarás, Gérvágás, Csiszolás (több variáns) | Manuális | Koppintás Start/Done |
| 3 | **Felületkezelés** | Fúrás, Ragasztó, Fóliázás | Manuális | Koppintás Start/Done |
| 4 | **Összeszerelés** | Él-lécezés/Kivágás, CNC Pánt-zár, Tok összerakás, Gér összerakás | Manuális, opcionális fotó | Koppintás + fotó upload |
| 5 | **Csomagolás** | Paknizás, Csomagolás | Manuális | "ZÖLD jelölés" → Kész |
| 6 | **Kiszállítható** | Kész termék → Raktár → Beépítés | Auto: Csomagolás=Kész → push | Push tulaj/sales |

**Fontos**: A 6 STAGE **összevont szintek** - a valós Munkamenet.pdf 17 mikro-fázisa + Folyamatok.xlsm alkatrész-kategóriánkénti Kísérőlevél-rendszer **VÁLTOZATLANUL az Excel-ben marad**. A mobil app nem helyettesíti, hanem kiegészíti (gyors STAGE-státusz + élő láthatóság).

## Feladatod: Implementation Plan

Készítsd el a következőket:

### 1. OpenAPI Contract Draft

**Endpointok** (példa struktura, finomítsd a DDD pattern szerint):

```yaml
POST /api/production/jobs                      # ProductionJob létrehozása (OrderConfirmed event után)
GET  /api/production/jobs                      # Műhelyvezető: aktív projektek listája
GET  /api/production/jobs/{jobId}              # Projekt részletei (6 STAGE state-tel)
PUT  /api/production/jobs/{jobId}/steps/{stepId}/start   # STAGE indítása (sárga)
PUT  /api/production/jobs/{jobId}/steps/{stepId}/complete # STAGE befejezése (zöld)
POST /api/production/jobs/{jobId}/steps/{stepId}/photo   # Opcionális fotó upload (Összeszerelés STAGE)
GET  /api/production/overview                  # Tulaj/sales: élő áttekintő (összes aktív projekt state)
```

**DTOs**:
- `ProductionJobDto` - jobId, projectName, deadline, currentStage, progress (%), csúszás-jelzés
- `WorkflowStepDto` - stepId, stageName, status (Queued/InProgress/Done), startedAt, completedAt
- `ProductionOverviewDto` - tulaj/sales view, összes aktív projekt state, csúszók kiemelve

**Event publikálás**:
- `ProductionJob.ShippingReady` → Inventory.ReserveForShipping
- `ProductionJob.ShippingReady` → Sales notification (Telegram/email - Viber kiváltás)

### 2. Backend Task Breakdown

DDD rétegek szerint:

#### Domain Layer
- [ ] `ProductionJob` aggregate root
  - [ ] `ProductionJobId` value object
  - [ ] `WorkflowStep` entity (6 STAGE)
  - [ ] `ProductionStatus` enum (Queued → ShippingReady)
  - [ ] FSM validáció (csak current step completable)
- [ ] Domain events:
  - [ ] `ProductionJobStarted`
  - [ ] `WorkflowStepCompleted`
  - [ ] `ProductionJobShippingReady`
- [ ] Value objects:
  - [ ] `WorkflowStepName` (Szabászat, Megmunkálás, stb.)
  - [ ] `ProductionDeadline`

#### Application Layer
- [ ] Commands:
  - [ ] `StartProductionJobCommand` + Handler
  - [ ] `CompleteWorkflowStepCommand` + Handler
  - [ ] `MarkAsShippingReadyCommand` + Handler
- [ ] Queries:
  - [ ] `GetProductionQueueQuery` + Handler (műhelyvezető UI)
  - [ ] `GetJobStatusForOwnerQuery` + Handler (tulaj/sales view)
  - [ ] `GetProductionJobByIdQuery` + Handler

#### Infrastructure Layer
- [ ] `ProductionDbContext` (EF Core)
- [ ] `ProductionJobRepository` (aggregate persistence)
- [ ] Event subscribers:
  - [ ] `CuttingCompletedEventHandler` (ADR-038 integráció)
  - [ ] `OrderConfirmedEventHandler` (ProductionJob creation trigger)

#### API Layer
- [ ] `ProductionController` (REST endpoints)
- [ ] OpenAPI spec

#### Integration Testing
- [ ] E2E test: OrderConfirmed → ProductionJob created
- [ ] E2E test: CuttingCompleted → Szabászat auto-complete
- [ ] E2E test: 6 STAGE manual completion → ShippingReady event
- [ ] E2E test: ShippingReady → Sales notification sent

### 3. Frontend Task Breakdown (jelöld meg, de ne implementáld - Frontend terminál fogja)

- [ ] `ProductionJobCard` component (touch-optimized, STAGE progress)
- [ ] `WorkflowStepStepper` component (6 STAGE visual stepper)
- [ ] `KioskMobileLayout` (minimal nav, full-screen STAGE view)
- [ ] `ProductionOverviewPage` (tulaj/sales: all projects state)
- [ ] SSE integration: `ProductionJobStatusChannel` (real-time push)

### 4. Integration Points (dokumentáld)

- **Bejövő események** (subscribes to):
  - `CuttingJob.CuttingCompleted` (ADR-038) → `ProductionJob.WorkflowStep["Szabászat"]` auto-complete
  - `OrderItem.OrderConfirmed` (Joinery/CRM) → `ProductionJob` creation
- **Kimenő események** (publishes):
  - `ProductionJob.ShippingReady` → `Inventory.ReserveForShipping`
  - `ProductionJob.ShippingReady` → Sales/tulaj notification (Telegram/email)
  - `ProductionJob.WorkflowStepCompleted` → Analytics timeline

### 5. Timeline Estimate (naptári napok)

- [ ] Domain + Application layer: 2 nap
- [ ] Infrastructure + API layer: 1 nap
- [ ] Integration tests: 1 nap
- [ ] Frontend (párhuzamos, Frontend terminál): 2 nap
- [ ] E2E integration + pilot test: 1 nap

**Total backend**: ~4 nap | **Total frontend**: ~2 nap | **Pilot test**: 1 nap
**Összesen**: ~5-6 nap (backend+frontend párhuzamos)

## Acceptance Criteria

- [ ] OpenAPI contract draft elkészült (`.yaml` vagy inline markdown)
- [ ] Backend task breakdown DDD rétegenként (Domain/Application/Infrastructure/API)
- [ ] Frontend task breakdown (jelölve, ne implementáld)
- [ ] Integration points dokumentálva (események, triggering)
- [ ] Timeline estimate (naptári napok)
- [ ] Implementation Plan DONE outbox (1-2 napon belül)

## Referencia Fájlok

- **Domain spec**: `/tmp/doorstar_domain_spec.md` (MSG-ROOT-038 korrigált verzió - már olvastam)
- **Cabinet validáció**: `/opt/spaceos/terminals/root/inbox/2026-07-08_040_cabinetvps--blocking-validation-lezrva.md`
- **VPS architektúra javaslat**: `/opt/spaceos/terminals/cabinet-bridge/inbox/2026-07-08_019_vps-doorstar-muhely-statusz-valasz-shopfloor-modul-jav.md`
- **Cutting integráció**: ADR-038 (`CuttingJob.CuttingCompleted` event, `ICuttingEventPublisher`)

## Notes

- **Ne implementáld még a kódot** - csak az Implementation Plan kell (OpenAPI + task breakdown + timeline)
- Cabinet várja a válaszunkat - gyors turnaround előnyben (1-2 nap)
- Ez egy **valós ügyfél-win** - Doorstar Kft. production-ready 2026-09-30 target (EPIC-DOORSTAR-SOFTLAUNCH)
- A 6 STAGE scope **végleges** - ne tervezz 17 mikro-fázist, az Excel-ben marad

---

📋 Root → Backend — Doorstar Production Workflow Implementation Plan (2026-07-08 22:30 UTC)
