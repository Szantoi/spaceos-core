# Q3 Szabászat Modul — Tervezési Javaslat (2026-06-22)

**Készítette:** Conductor
**Státusz:** DRAFT
**Cél:** 2. ügyfél fogadása Q3-ban (lapszabász KKV)

---

## Executive Summary

A **Cutting modul backend PRODUCTION READY** (939 teszt, 8 aggregate, komplett API). A TOP 1-3 frontend implementáció folyamatban van (FE-061/062/063).

**Q3 célhoz szükséges:** 3 HIÁNYZÓ feature + 2 kiegészítő optimalizáció

---

## Cutting Modul Jelenlegi Állapot (Audit 2026-06-22)

### ✅ Backend — KÉSZ (939 teszt)

**8 Aggregate Root:**
1. CuttingSheet — szabászati lapok beküldése
2. CuttingPlan — 14 napos tervezés, FSM (Draft→Published→Frozen→Closed)
3. DailyCuttingPlan — napi terv
4. PanelReservation — panel foglalás (Inventory integráció)
5. PriorityProfile — prioritási szabályok
6. CuttingExecution — végrehajtás tracking
7. AdapterHealthRecord — adapter health monitoring
8. TenantCuttingProviderConfig — multi-provider konfiguráció

**API Endpoints (6 csoport):**
- `/api/cutting/` — sheets, nesting, waste report
- `/api/cutting/planning/` — create/update/publish/freeze/close
- `/api/cutting/executions/` — végrehajtás tracking
- `/api/cutting/analytics/` — OEE, waste, material usage
- `/api/cutting/adapters/` — adapter admin
- `/cutting/api/plans/{date}/assign-batch` — gép kiosztás (CUTTING-054 ✅)

**Integrations:**
- ✅ Inventory — panel reservation
- ✅ Joinery — `/internal/ingest-order` (rendelés→szabás pipeline)
- ✅ Nesting.Algorithms — NuGet library (32 teszt)

### ✅ Frontend — TOP 1-3 COMPLETE (2026-06-22)

| Feature | MSG ID | Státusz |
|---|---|---|
| Design→Cutting workflow | MSG-FE-061 | ✅ DONE (API integration fixed) |
| Nesting vizualizáció | MSG-FE-062 | ✅ DONE (SVG canvas, zoom/pan, stats) |
| Machine Scheduling UI | MSG-FE-063 | ✅ DONE (4-column Kanban, drag-and-drop, 468 LoC) |

**Quality metrics:**
- Frontend tests: 941 pass (19 új Cutting-specific tests)
- Backend tests: 994 pass (Cutting + Identity endpoints ready)
- TypeScript errors: 0
- Build: successful

---

## Q3 Hiányzó Feature-ök (2. Ügyfél Céljához)

### 🔴 PRIORITY 1: Customer Self-Service Portal (B2C)

**Use case:** Külső megrendelők (nem saját tenant) beküldik a szabás-rendelést online.

**Hiányzó komponensek:**
1. **Backend:**
   - `POST /public/cutting/quote-request` (unauthenticated)
   - Quote FSM: QuoteRequest → PendingReview → Approved → ConvertedToOrder
   - Email notification (Brevo SMTP ✅ van)
2. **Frontend:**
   - Public landing page + quote form (nincs login gate)
   - Quote status tracking (magic link vagy simple token)

**Effort:** 3-4 nap (1.5 nap BE + 2 nap FE)
**Blocking:** NEM — párhuzamosan implementálható

---

### 🟡 PRIORITY 2: Pricing Integration (Lapszabász Szolgáltatás-Árazás)

**Use case:** Kereskedői/lapszabász árazás szabás-paraméterek alapján (m² + élzárás + technológiai felárak).

**Hiányzó komponensek:**
1. **Backend:**
   - `GET /api/cutting/pricing/calculate` (input: materialType, area, edging, thickness → output: price breakdown)
   - Pricing Rule Engine (stored in DB vagy config)
2. **Frontend:**
   - Trade World PoC már van (`page-trade-2.jsx` prototípusban)
   - Integráció a Quote Request form-ba

**Effort:** 2-3 nap (1 nap BE pricing engine + 1-2 nap FE integration)
**Blocking:** NEM — párhuzamosan implementálható

---

### 🟢 PRIORITY 3: Production Floor Integration (ShopFloor World)

**Use case:** Gépkezelők látják a gép-queue-t, kiválasztják a következő munkát, jelzik a befejezést.

**Hiányzó komponensek:**
1. **Backend:**
   - `GET /api/cutting/machines/{machineId}/queue` (assigned batches)
   - `POST /api/cutting/executions/{executionId}/start`
   - `POST /api/cutting/executions/{executionId}/complete`
   - SignalR hub már van: `ExecutionHub` ✅
2. **Frontend:**
   - ShopFloor World mock-kal van (FE-033 ✅)
   - API integration: queue lista + kiosk workflow

**Effort:** 2 nap (0.5 nap BE endpoints + 1.5 nap FE integration)
**Blocking:** TOP 3 Machine Scheduling (MSG-FE-063) után indulhat

---

## OPTIMALIZÁCIÓK (Nice-to-Have)

### 🔵 OPT-1: Multi-Tenant Nesting Optimization

**Use case:** Több tenant rendelésének összevonása (pl. 3 kis lapszabász közös táblába optimalizál → kevesebb hulladék).

**Komponensek:**
- Backend: `POST /api/cutting/planning/multi-tenant-batch` (tenant[] input)
- Nesting.Algorithms bővítés multi-tenant scoring-gal

**Effort:** 5-6 nap (komplex)
**Blocking:** NEM
**Priority:** DEFER (Q4)

---

### 🔵 OPT-2: Quality Control & Rework

**Use case:** Hibás szabás jelentése + újraszabás workflow.

**Komponensek:**
- Backend: `POST /api/cutting/executions/{id}/report-defect` + Rework FSM
- Analytics: defect rate tracking

**Effort:** 3 nap
**Blocking:** NEM
**Priority:** DEFER (Q4)

---

## JAVASOLT IMPLEMENTÁCIÓS TERV (Q3)

### Track A: Customer Portal (Week 1-2) — HIGH

| Task | Assignee | Effort | Blocking |
|---|---|---|---|
| Quote Request API (POST /public/cutting/quote-request) | Backend | 1.5 nap | — |
| Public Quote Form + Status Tracking | Frontend | 2 nap | Backend Quote API |
| Email notification integration (Brevo) | Backend | 0.5 nap | — |

**Total:** 4 nap (1 backend + 1 frontend párhuzamosan → 2 nap átfutás)

---

### Track B: Pricing Engine (Week 2-3) — MEDIUM

| Task | Assignee | Effort | Blocking |
|---|---|---|---|
| Pricing Rule Engine + API | Backend | 1 nap | — |
| Trade World integration (Quote Form árazás) | Frontend | 1-2 nap | Pricing API |

**Total:** 3 nap

---

### Track C: ShopFloor Integration (Week 3-4) — MEDIUM

| Task | Assignee | Effort | Blocking |
|---|---|---|---|
| Machine Queue + Execution Start/Complete API | Backend | 0.5 nap | TOP 3 (MSG-FE-063) |
| ShopFloor World API integration | Frontend | 1.5 nap | Backend API |

**Total:** 2 nap

---

## ÖSSZESÍTÉS

| Metric | Value |
|---|---|
| **Meglévő teszt** | 939 (backend) + 742 (frontend) = **1,681** |
| **Szükséges új feature** | 3 (Customer Portal, Pricing, ShopFloor) |
| **Becsült implementációs idő** | 9 munkanap (3 track párhuzamosan → **~2 hét átfutás**) |
| **2. ügyfél típusa** | Lapszabász KKV (B2C customer portal prioritás) |
| **Deferred (Q4)** | Multi-tenant optimization, Quality control |

---

## KÖVETKEZŐ LÉPÉSEK

1. **Conductor:** Backend task kiadása (MSG-BACKEND-xxx)
2. **Frontend:** TOP 3 befejezése után ShopFloor + Quote Form indítása
3. **Root:** 2. ügyfél azonosítás + onboarding tervezés
4. **Architect:** (opcionális) Pricing Rule Engine design review

---

**Készítette:** Conductor
**Dátum:** 2026-06-22
**Frissítve:** 2026-06-22 19:45 (TOP 1-3 COMPLETE)
**Státusz:** AWAITING ROOT APPROVAL

---

## ⚠️ FRISSÍTÉS (2026-06-22 19:45)

**Cutting Module TOP 1-2-3 Trilogy:** ✅ **ALL COMPLETE**

Azóta ami a javaslat készült (délután), a Frontend terminál mind a 3 TOP feature-t implementálta és DONE-olta:
- TOP 1 (Design→Cutting workflow): API integration fix, 5 perc implementáció
- TOP 2 (Nesting Visualization): Már 100%-ban kész volt
- TOP 3 (Machine Scheduling UI): 4-column Kanban, drag-and-drop, 468 LoC új kód, 2 óra implementáció

**Backend supportáló endpoint-ok:** ✅ mind READY
- `POST /cutting/api/plans/{date}/assign-batch` (MSG-BACKEND-022)
- `GET /identity/users?role={role}` (MSG-BACKEND-023)

**Cutting modul production-readiness:** **100% READY Doorstar számára**

A Q3 javaslat továbbra is érvényes a **2. ügyfél** (lapszabász KKV) célhoz.
