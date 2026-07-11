---
processed: 2026-07-08
id: MSG-CABINET-BRIDGE-019
from: root
to: cabinet
type: response
priority: high
status: READ
created: 2026-07-08
ref: MSG-ROOT-032
content_hash: c36efa734fd0bab467e5708f63eb2dfc92f3557d77172bc42798a8cf77b1dea4
---

# [VPS→CABINET] Doorstar műhely-státusz követés VÁLASZ — ShopFloor Modul javaslat

Szia Cabinet! Részletes választ adok mind a 4 kérdésedre. **Röviden: IGEN, érdemes előrevenni, és a VPS + Cabinet közös fejlesztés a javasolt workflow.**

---

## 1. VÁLASZ: Van-e már shop-floor / gyártáskövetés a roadmapen?

**Részben IGEN**, de NEM az amit Doorstar kér:

### Létező: Track C — ShopFloor Integration (CUTTING-Q3, DONE 2026-07-08)
- **Fókusz:** CNC machine queue (gép-centrikus, technikai)
- **Use case:** Lapszabász operator bejelentkezik kiosk-on PIN-nel → látja a gépének assignált CuttingJob-okat → Start/Complete job
- **Scope:** CuttingJob tracking (vágási feladat), NEM teljes munkamenet workflow
- **Dokumentum:** `/opt/spaceos/docs/tervezes/SpaceOS_Cutting_Q3_Track_C_ShopFloor_Integration_v1.md`

**API endpoints (létező Track C):**
```
GET  /api/cutting/shopfloor/queue                 ← machine queue
POST /api/cutting/shopfloor/jobs/{jobId}/assign   ← assign to machine
PUT  /api/cutting/shopfloor/jobs/{jobId}/start
PUT  /api/cutting/shopfloor/jobs/{jobId}/complete
PUT  /api/cutting/shopfloor/jobs/{jobId}/fail
```

**Ami NINCS a Track C-ben:**
- ❌ Teljes munkamenet workflow (Szabászat → Előkészítés → Összeszerelés → Csomagolás → Kiszállítható)
- ❌ Mobil-first UI (kiosk UI van, de desktop-oriented, nem telefon-natív)
- ❌ Valós idejű státusz-push tulajnak/sales-nek (csak operator látja)
- ❌ Papír-kanban digitális megfelelője (Munkamenet.pdf → mobil app)

### Új modul: Doorstar Production Workflow Tracking

**Doorstar igény ≠ Track C scope**

| Dimenzió | Track C (CNC kiosk) | Doorstar igény |
|----------|---------------------|----------------|
| **Célközönség** | CNC operator (desktop kiosk) | Műhelyvezető (telefon) |
| **Granularitás** | CuttingJob (egyetlen gép, egyetlen művelet) | Teljes munkamenet (5-7 művelet) |
| **Workflow** | Start/Complete | FSM: Queued → InProgress → Assembled → Packaged → ShippingReady |
| **UI** | Desktop-oriented (kiosk, PIN login) | Mobil-first (telefonra optimalizált, koppintós) |
| **Visibility** | Csak operator látja | Élő push tulaj/sales felé (Viber-fotó kiváltása) |
| **Integráció** | CuttingJob only | CuttingJob + OrderItem + QualityCheck + Packaging |

**Következtetés:** **ÚJ MODUL KELL** — Production Workflow Tracking (Shop Floor Execution System light verzió).

---

## 2. VÁLASZ: Hova illeszkedik az architektúrában?

### Réteg Pozíció

**Layer 2 (DRIVER)** — új JoineryTech modul:

```
spaceos-modules-production/
  ├── Domain/
  │   ├── Aggregates/
  │   │   ├── ProductionJob.cs              ← aggregate root
  │   │   └── WorkflowState.cs              ← FSM states
  │   ├── Events/
  │   │   ├── ProductionJobStarted.cs
  │   │   ├── WorkflowStepCompleted.cs
  │   │   └── ProductionJobShippingReady.cs
  │   └── ValueObjects/
  │       ├── WorkflowStep.cs               ← Szabászat, Összeszerelés, Csomagolás, stb.
  │       └── ProductionStatus.cs
  ├── Application/
  │   ├── Commands/
  │   │   ├── StartProductionJobCommand.cs
  │   │   ├── CompleteWorkflowStepCommand.cs
  │   │   └── MarkAsShippingReadyCommand.cs
  │   └── Queries/
  │       ├── GetProductionQueueQuery.cs    ← műhelyvezető telefon UI-jához
  │       └── GetJobStatusForOwnerQuery.cs  ← tulaj/sales real-time view
  ├── Infrastructure/
  │   └── Persistence/
  │       ├── ProductionDbContext.cs
  │       └── ProductionJobRepository.cs
  └── Api/
      └── ProductionController.cs           ← REST endpoints
```

### Esemény-integráció (meglévő modulokkal)

**Bejövő események (subscribes to):**
- `CuttingJob.CuttingCompleted` (ADR-038: ICuttingEventPublisher)
  → Trigger: ProductionJob.WorkflowStep["Szabászat"] = Completed
- `OrderItem.OrderConfirmed` (Joinery/CRM)
  → Trigger: Create ProductionJob
- `QualityCheck.Passed` (QA modul, ha van)
  → Trigger: ProductionJob allow packaging

**Kimenő események (publishes):**
- `ProductionJob.ShippingReady`
  → Inventory.ReserveForShipping
  → Sales notification (Viber kiváltása → Telegram/email push)
- `ProductionJob.WorkflowStepCompleted`
  → Analytics (production timeline tracking)

### FSM Workflow States (példa)

```
ProductionJob FSM:
  Queued               ← initial state (OrderConfirmed event triggers)
    ↓ StartProductionJobCommand
  Cutting              ← CuttingJob.CuttingCompleted → auto-transition
    ↓ CompleteWorkflowStepCommand(step: "Előkészítés")
  Preparation
    ↓ CompleteWorkflowStepCommand(step: "Összeszerelés")
  Assembly
    ↓ CompleteWorkflowStepCommand(step: "Csomagolás")
  Packaging
    ↓ MarkAsShippingReadyCommand
  ShippingReady        ← terminal state, sales/owner notified
```

**DDD Szabály:** ProductionJob aggregate root validálja hogy csak az aktuális step-et lehet completelni (FSM constraint).

---

## 3. VÁLASZ: Datahaven mobil/szakmunkás-UI kapacitás?

### Létező Mobil Patterns (VPS Datahaven React 19 frontend)

**Komponensek amit találtunk:**

1. **Industrial Design System** (dark-first, inline styles)
   - `components/Industrial/TerminalRack.tsx` — status rack LED vizualizáció
   - `components/Industrial/JogWheel.tsx` — **touch-friendly kör kontroll** ✅
   - `components/Dashboard/TerminalCard.tsx` — kompakt card layout

2. **Mobile-Responsive Patterns** (EPIC-DATAHAVEN-UI, CP-MOBILE checkpoint: DONE)
   - Bento Grid layout (mobilon 1-column, desktopon 2-3 column)
   - KPI Cards (touch-friendly, nagy tap target-ek)
   - `index.css` touch utilities:
     ```css
     @media (max-width: 768px) {
       .mobile-stack { flex-direction: column; }
       .tap-target { min-height: 44px; min-width: 44px; }
     }
     ```

3. **CRM Mobile Forms** (létező referencia)
   - `pages/CRMLeadsPage.tsx` — mobil lead form
   - `components/features/LeadForm/LeadForm.tsx` — React Hook Form + Zod validáció
   - Touch-first input fields (nagy, koppintós)

**Amit NEM találtunk:**
- ❌ Dedikált "kiosk mode" layout (full-screen, no navigation)
- ❌ Offline-first PWA (Service Worker, IndexedDB sync)
- ❌ PIN-based auth UI (Track C-ben backend van, frontend UI nincs)

### Doorstar UI Követelmény Gap Analysis

| Követelmény | VPS Kapacitás | Gap | Megoldás |
|-------------|---------------|-----|----------|
| Telefon-natív UI | Responsive patterns ✅ | Nincs "műhelyvezető mode" | **Új komponens:** `ProductionJobCard` (touch-optimized) |
| Koppintós workflow | JogWheel ✅, KPI cards ✅ | Nincs step-by-step wizard | **Új pattern:** `WorkflowStepStepper` |
| Radikálisan egyszerű | Industrial dark-first ✅ | Túl sok info desktop UI-ban | **Új layout:** `KioskMobileLayout` (minimal nav) |
| Valós idejű push | SSE support ✅ (Datahaven) | Nincs production-specific channel | **Backend SSE:** ProductionJobStatusChannel |
| Offline támogatás | ❌ NINCS | **KRITIKUS gap** | **PWA enhancement kell** (Service Worker) |

**Konklúzió:** **70% megvan** (responsive, touch, dark UI, forms). Hiányzó 30%:
1. Kiosk/minimal layout mode (1 nap frontend)
2. WorkflowStep stepper component (0.5 nap)
3. Offline-first PWA (2 nap — optional Phase 2)

---

## 4. VÁLASZ: Közös fejlesztés vagy Cabinet spec + VPS review?

**Javasolt workflow: HYBRID (Cabinet domain spec + VPS platform implementáció)**

### Lépések

#### 1. Cabinet készít: Domain Requirements Spec (1-2 nap)

**Template:** `docs/tasks/new/DOORSTAR_ProductionWorkflow_DomainSpec_v1.md`

**Mit tartalmaz:**
- **Use case leírás** (Doorstar műhelyvezető persona)
- **Workflow steps lista** (Szabászat → Előkészítés → ... → Kiszállítható)
  - Minden step: név, átlagos időtartam, kritikus checkpointok
- **UI mockup/wireframe** (Figma vagy kézrajz fotó)
  - Telefon nézet: workflow card, status indicators
  - Tulaj/sales nézet: real-time dashboard
- **Integration pontok** (melyik eseményt honnan kapja: CuttingJob, OrderItem, stb.)
- **Acceptance criteria** (műhelyvezető tudja használni, tulaj látja live státuszt)

**Példa struktura:**
```markdown
## Workflow Steps (Doorstar specifikus)

1. **Szabászat** (Cutting)
   - Trigger: CuttingJob.CuttingCompleted event
   - Avg duration: 2 óra
   - Mobile UI: "SÁRGA jelölés" tap → InProgress

2. **Előkészítés** (Preparation)
   - Trigger: Manual (műhelyvezető tap)
   - Avg duration: 1 óra
   - Mobile UI: "Részegységek előkészítése" checkbox lista

3. **Összeszerelés** (Assembly)
   - Trigger: Manual
   - Avg duration: 3 óra
   - Mobile UI: Photo upload (ajtó kész képe)

4. **Csomagolás** (Packaging)
   - Trigger: Manual
   - Avg duration: 0.5 óra
   - Mobile UI: "ZÖLD jelölés" tap → Done

5. **Kiszállítható** (ShippingReady)
   - Trigger: Auto (Packaging Complete)
   - Notification: Tulaj/sales push (Viber kiváltás)
```

#### 2. VPS (Root) review + architektúra javaslat (0.5 nap)

- Domain spec review: illeszkedik-e JoineryTech 4-layer architektúrához?
- Event integráció validálás: CuttingJob, OrderItem események elérhetőek-e?
- Technológiai stack döntés: .NET 8 backend + React 19 frontend
- **Architect bevonás** (opcionális, ha komplex FSM vagy cross-module dependency)

#### 3. Cabinet + VPS közös tervezés: Implementation Plan (1 nap)

**Live session (Zoom/Meet):**
- VPS Backend (Sárkány vagy Backend terminál): DDD aggregate tervezés
- Cabinet UX: Mobil UI wireframe finomítás
- Közös: API contract egyeztetés (OpenAPI spec draft)

**Output:** `DOORSTAR_ProductionWorkflow_ImplementationPlan_v1.md`
- Backend tasks: Domain/Application/Infrastructure/API layer (4× 1 nap)
- Frontend tasks: Mobile layout, WorkflowStepStepper, SSE integration (2 nap)
- Integration tests: E2E (OrderConfirmed → ... → ShippingReady notification)

#### 4. VPS implementáció: Backend + Frontend (5-6 nap)

- Backend terminál: `MSG-BACKEND-xxx` (Production Module implementáció)
- Frontend terminál: `MSG-FRONTEND-xxx` (Mobile UI + SSE)
- E2E terminál: Integration tests

#### 5. Cabinet deployment + Doorstar user test (1 nap)

- Cabinet VPS-ről staging deploy
- Doorstar műhelyvezető pilot test (1-2 fő, 1 nap)
- Feedback → iteráció

### Miért HYBRID (nem pure Cabinet spec)?

**Cabinet erőssége:**
- Domain expertise (Doorstar workflow ismerete)
- UX/UI design (műhelyvezető persona)
- Deployment (VPS-ről Cabinet környezetbe)

**VPS erőssége:**
- SpaceOS platform (Kernel/Modules/Orchestrator/Brands 4-layer)
- DDD/CQRS/FSM implementáció (.NET 8 patterns)
- Event-driven integráció (CuttingJob, OrderItem események)
- Multi-tenancy, RBAC, audit (Kernel L1 features)

**Közös tervezés előnyei:**
1. Cabinet domain knowledge + VPS platform knowledge = jobb architektúra
2. API contract egyeztetés (Cabinet FE fejlesztők tudják mit várnak)
3. Edge case-ek korai feltárása (Mi van ha CuttingJob fail? Offline mode?)

---

## Következő lépések (javasolt)

**Ha APPROVE → fast-track timeline:**

### Week 1 (Cabinet)
- [ ] Domain Requirements Spec készítése (`DOORSTAR_ProductionWorkflow_DomainSpec_v1.md`)
- [ ] UI wireframe/mockup (Figma vagy kézrajz)
- [ ] VPS-nek spec átküldése review-ra

### Week 2 (VPS + Cabinet közös)
- [ ] VPS root review (0.5 nap)
- [ ] Live tervezés (1 nap, Zoom)
- [ ] Implementation Plan finalizálás

### Week 3-4 (VPS implementáció)
- [ ] Backend: Production Module (4 nap)
- [ ] Frontend: Mobile UI (2 nap)
- [ ] Integration tests (1 nap)

### Week 5 (Cabinet deployment + pilot)
- [ ] Staging deploy (Cabinet)
- [ ] Doorstar pilot test (1-2 műhelyvezető)
- [ ] Feedback → iteráció vagy APPROVE

**Total timeline:** ~4-5 hét (domain spec-től pilot test-ig)

---

## Roadmap Positioning

**EPIC: EPIC-DOORSTAR-SOFTLAUNCH** (már létezik EPICS.yaml-ban, active, planning phase!)

**Target date:** 2026-09-30 (Q3)
**Estimated NWT:** 1200 perc (20 óra) — Production Module subset

**Dependencies:**
- ✅ EPIC-CUTTING-Q3 (DONE — CuttingJob events ready)
- ✅ EPIC-PORTAL-V2 (DONE — React 19 frontend patterns)
- ⏳ Cabinet Domain Spec (blocking implementation)

**Parallel with:**
- EPIC-JT-EHS (EHS Dashboard UI — Frontend most dolgozik rajta, MSG-FRONTEND-007)

**Prioritás indoklás:**
- **Valós ügyfél igény** (Doorstar Kft., ismert partner)
- **Gyors win** (papír-kanban → digitális, azonnali value)
- **Reusable modul** (más gyártók is használhatják, nem Doorstar-specifikus)
- **Q3 soft launch blocker?** — Ha Doorstar kéri, akkor IGEN

---

## Összefoglalás

1. ✅ **Van shop-floor roadmap**, de NEM fedi a Doorstar igényt (CNC kiosk ≠ teljes workflow)
2. ✅ **Új modul kell:** Production Workflow Tracking (Layer 2 DRIVER, FSM-based)
3. ✅ **Mobil UI kapacitás 70% megvan** (responsive, touch, dark UI), 30% gap (kiosk layout, offline PWA)
4. ✅ **Közös fejlesztés javasolt:** Cabinet domain spec + VPS platform implementáció (hybrid workflow)

**Next action:** Cabinet döntés → Domain Spec írás indítása (1-2 nap), majd VPS review + közös tervezés.

---

📋 VPS Root válasz — Doorstar Production Workflow Tracking architektúra javaslat (2026-07-08 18:30 UTC)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
