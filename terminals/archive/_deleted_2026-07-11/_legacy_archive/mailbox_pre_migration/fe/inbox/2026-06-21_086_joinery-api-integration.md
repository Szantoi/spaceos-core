---
id: MSG-FE-086
from: root
to: fe
type: task
priority: high
status: READ
model: sonnet
ref: docs/planning/ideas/2026-06-16_003_joinery-api-integration.md
created: 2026-06-21
---

# Joinery API Integration — Material Requisition & Daily Cutting Plan

## Összefoglaló

Integráld a Joinery modul backend API-jait a Portal frontend-be. A backend API-k már készen vannak (port 5002, 5004), de a frontend még mock data-val dolgozik. Ez az integráció-gap bezárása kritikus a Doorstar Soft Launch számára.

---

## Backend API-k (READY)

A következő endpointok már implementálva vannak és működnek:

### Joinery Module (port 5002)
- `GET /api/orders/{id}/material-req` — anyagszükséglet (BOM)
- `GET /api/orders/{id}/hardware-list` — ferdeség, festékadatok
- `GET /api/orders/{id}/process-plan` — termelési eljárás lépések

### Cutting Module (port 5004)
- `POST /api/cutting/plans` — napi vágási terv generálása
- `GET /api/cutting/plans?date={YYYY-MM-DD}` — vágási terv lekérdezés

---

## Frontend Integráció (TODO)

### 1. OrderDetailPage.tsx komponens (2 óra)

**Új komponensek:**
- `MaterialRequisitionTable` — anyagszükséglet lista (anyag, mennyiség, költség)
- `HardwareSpecsCard` — ferdeség, festék specifikációk

**API hookok:**
```typescript
useEffect(() => {
  fetchMaterialReq(orderId)
    .then(data => setMaterialReq(data))
}, [orderId])

useEffect(() => {
  fetchHardwareList(orderId)
    .then(data => setHardwareList(data))
}, [orderId])
```

**API hívások:**
- `GET /api/orders/{id}/material-req`
- `GET /api/orders/{id}/hardware-list`

### 2. ProductionPage.tsx komponens (2 óra)

**Új funkciók:**
- "Generate Daily Cutting Plan" gomb
- Cutting plan table renderelés (poll-based)

**API hookok:**
```typescript
const handleGeneratePlan = async () => {
  await fetch('/api/cutting/plans', {
    method: 'POST',
    body: JSON.stringify({
      date: selectedDate,
      capacity: machineCapacity,
      orders: selectedOrders
    })
  })

  // Poll for results
  startPolling()
}

const pollCuttingPlan = () => {
  fetch(`/api/cutting/plans?date=${selectedDate}`)
    .then(data => setCuttingPlan(data))
}
```

**API hívások:**
- `POST /api/cutting/plans { date, capacity, orders[] }`
- `GET /api/cutting/plans?date={today}`

### 3. Orchestrator Route Verification (1 óra)

Verify hogy az Orchestrator (port 3000) helyesen proxy-zza a kéréseket:
```typescript
// Verify these routes exist in orchestrator:
GET /api/orders/:id/material-req → joinery_proxy (port 5002)
GET /api/orders/:id/hardware-list → joinery_proxy (port 5002)
POST /api/cutting/plans → cutting_proxy (port 5004)
GET /api/cutting/plans → cutting_proxy (port 5004)
```

Ha hiányzik valamelyik route, jelezd ORCH terminálnak.

### 4. Integration Tests (1.5 óra)

**E2E test szekvencia:**
1. OrderDetail oldal → fetch material-req → verify rendering
2. OrderDetail oldal → fetch hardware-list → verify rendering
3. ProductionPage → POST cutting plan → verify API call
4. ProductionPage → poll GET cutting plans → verify results

**Test fájl:** `frontend/joinerytech-portal/src/pages/__tests__/JoineryIntegration.test.tsx`

---

## Definition of Done

### Frontend komponensek
- [ ] `MaterialRequisitionTable` komponens létezik és renderel
- [ ] `HardwareSpecsCard` komponens létezik és renderel
- [ ] `OrderDetailPage.tsx` használja a komponenseket
- [ ] `ProductionPage.tsx` "Generate Plan" gomb működik
- [ ] Cutting plan polling működik (5 sec interval)

### API integráció
- [ ] `GET /api/orders/{id}/material-req` hook létezik
- [ ] `GET /api/orders/{id}/hardware-list` hook létezik
- [ ] `POST /api/cutting/plans` hook létezik
- [ ] `GET /api/cutting/plans?date=...` hook létezik
- [ ] Orchestrator routing verified (vagy ORCH-nak jelezve)

### Tesztek
- [ ] E2E test: OrderDetail → material-req → rendering ✅
- [ ] E2E test: OrderDetail → hardware-list → rendering ✅
- [ ] E2E test: ProductionPage → POST plan → poll → results ✅
- [ ] TypeScript build: 0 errors
- [ ] Vitest suite: all tests passing

### Mock removal
- [ ] `OrderDetailPage.tsx`: mock material-req removed
- [ ] `OrderDetailPage.tsx`: mock hardware-list removed
- [ ] `ProductionPage.tsx`: mock cutting plans removed

---

## Üzleti kontextus

**MES Standard (MESA, ISA-95):**
- Material planning + BOM management = baseline manufacturing operations
- Költség kontroll: asztalosiparos KKV-k anyag selejtezésre sokszor vakítanak
- Valós lista → rossz vásárlások csökkentése

**Doorstar Soft Launch:**
- Termelési paraméterek (ferdeség, anyag) ma Viber-en beszélgetnek
- Digitális BOM = verseny előny
- Gyártók valós anyaglistákat látnak (költség, raktár-szint)

---

## Constraints

1. **Cutting plan ID:** Nem adunk id-t explicit URL param-ben — csak POST { date, orders } és GET query ?date=YYYY-MM-DD
2. **Batch material-req:** Egy order = egy material-list. Ha multi-order BOM kell, Phase 2
3. **Real-time updates:** Polling-based (5 sec interval), WebSocket Phase 2

---

## Blocker

Nincs. Backend API-k ready (E7 modul), csak frontend integráció hiányzik.

---

## Referenciák

- **Planning idea:** `docs/planning/ideas/2026-06-16_003_joinery-api-integration.md`
- **Joinery backend:** `backend/spaceos-modules-joinery/` (port 5002)
- **Cutting backend:** `backend/spaceos-modules-cutting/` (port 5004)
- **Orchestrator:** `backend/spaceos-orchestrator/` (port 3000)

---

**Estimated effort:** 6.5 óra (2h frontend + 2h hooks + 1h routing + 1.5h tests)

**Priority:** HIGH — Doorstar Soft Launch kritikus path

**Model:** Sonnet (kód írás szükséges)

---

Kérdések esetén eszkalálj Root-nak vagy Conductor-nak.
